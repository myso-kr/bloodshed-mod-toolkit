using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Net
{
    internal sealed class LobbyManager
    {
        // Steam Callbacks — GC 방지 (IL2CPP 필수)
        private Callback<P2PSessionRequest_t>?      _cbP2PRequest;
        private Callback<P2PSessionConnectFail_t>?  _cbP2PFail;
        private Callback<GameLobbyJoinRequested_t>? _cbJoinRequested;
        private Callback<LobbyDataUpdate_t>?        _cbLobbyDataUpdate;
        private CallResult<LobbyCreated_t>?         _crLobbyCreated;
        private CallResult<LobbyEnter_t>?           _crLobbyEnter;

        private readonly P2PTransport _transport;
        private readonly PacketRouter  _router;

        // Heartbeat
        private readonly System.Collections.Generic.Dictionary<CSteamID, float> _lastHeartbeat;
        private float _heartbeatTimer;
        private const float HeartbeatInterval = 30f;
        public  const float HeartbeatTimeout  = 60f;

        // Guest 재시도
        private float _retryTimer;
        private const float RetryInterval = 5f;

        // 로비 멤버 스캔
        private float _memberScanTimer;
        private const float MemberScanInterval = 2f;

        public LobbyManager(
            P2PTransport transport,
            PacketRouter router,
            System.Collections.Generic.Dictionary<CSteamID, float> lastHeartbeat)
        {
            _transport     = transport;
            _router        = router;
            _lastHeartbeat = lastHeartbeat;
        }

        public void Initialize()
        {
            Action<P2PSessionRequest_t>      onReq       = OnP2PSessionRequest;
            Action<P2PSessionConnectFail_t>  onFail      = OnP2PConnectFail;
            Action<GameLobbyJoinRequested_t> onJoin      = OnGameLobbyJoinRequested;
            Action<LobbyDataUpdate_t>        onLobbyData = OnLobbyDataUpdate;

            _cbP2PRequest      = Callback<P2PSessionRequest_t>.Create(onReq);
            _cbP2PFail         = Callback<P2PSessionConnectFail_t>.Create(onFail);
            _cbJoinRequested   = Callback<GameLobbyJoinRequested_t>.Create(onJoin);
            _cbLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(onLobbyData);
        }

        public void Dispose()
        {
            _cbP2PRequest?.Dispose();
            _cbP2PFail?.Dispose();
            _cbJoinRequested?.Dispose();
            _cbLobbyDataUpdate?.Dispose();
            _crLobbyCreated?.Dispose();
            _crLobbyEnter?.Dispose();
        }

        public void CreateLobby(int maxPlayers = 4)
        {
            Plugin.Log.LogInfo($"[LobbyManager] 로비 생성 (최대 {maxPlayers}인)...");
            Action<LobbyCreated_t, bool> onCreated = OnLobbyCreated;
            _crLobbyCreated = CallResult<LobbyCreated_t>.Create(onCreated);
            var handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
            _crLobbyCreated.Set(handle);
        }

        public void JoinLobby(CSteamID lobbyId)
        {
            Plugin.Log.LogInfo($"[LobbyManager] 로비 참가: {lobbyId}");
            Action<LobbyEnter_t, bool> onEnter = OnLobbyEnter;
            _crLobbyEnter = CallResult<LobbyEnter_t>.Create(onEnter);
            var handle = SteamMatchmaking.JoinLobby(lobbyId);
            _crLobbyEnter.Set(handle);
        }

        public void LeaveLobby()
        {
            if (!CoopState.IsEnabled) return;
            try
            {
                foreach (var peer in CoopState.Peers)
                    SteamNetworking.CloseP2PSessionWithUser(peer);
                if (CoopState.LobbyId != CSteamID.Nil)
                    SteamMatchmaking.LeaveLobby(CoopState.LobbyId);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[LobbyManager] LeaveLobby Steam 예외 (종료 중?): {e.Message}");
            }
            CoopState.Reset();
            PlayerSyncHandler.Reset();
            _lastHeartbeat.Clear();
            _heartbeatTimer = 0f;
            Plugin.Log.LogInfo("[LobbyManager] 로비 퇴장");
        }

        public void Tick(float deltaTime, float now)
        {
            if (!CoopState.IsEnabled) return;

            if (CoopState.IsHost && CoopState.LobbyId != CSteamID.Nil)
            {
                _memberScanTimer += deltaTime;
                if (_memberScanTimer >= MemberScanInterval)
                {
                    _memberScanTimer = 0f;
                    ScanAndRegisterLobbyMembers();
                }
            }

            if (!CoopState.IsHost && !CoopState.IsConnected && CoopState.LobbyId != CSteamID.Nil)
            {
                _retryTimer += deltaTime;
                if (_retryTimer >= RetryInterval)
                {
                    _retryTimer = 0f;
                    RetryConnectToHost();
                }
            }

            if (CoopState.IsConnected)
            {
                _heartbeatTimer += deltaTime;
                if (_heartbeatTimer >= HeartbeatInterval)
                {
                    _heartbeatTimer = 0f;
                    _transport.BroadcastReliable(HeartbeatPacket.Encode());
                }
                CheckTimeouts(now);
            }
        }

        public void OnHeartbeatReceived(CSteamID from, float now)
        {
            _lastHeartbeat[from] = now;
        }

        public void TrackPeerActivity(CSteamID from, float now)
        {
            if (CoopState.Peers.Contains(from))
                _lastHeartbeat[from] = now;
        }

        // ── Steam 콜백 ────────────────────────────────────────────────────────
        private void OnP2PSessionRequest(P2PSessionRequest_t parm)
        {
            Plugin.Log.LogInfo($"[LobbyManager] P2P 세션 요청 수락: {parm.m_steamIDRemote}");
            SteamNetworking.AcceptP2PSessionWithUser(parm.m_steamIDRemote);
        }

        private void ScanAndRegisterLobbyMembers()
        {
            var myId = SteamUser.GetSteamID();
            int n    = SteamMatchmaking.GetNumLobbyMembers(CoopState.LobbyId);

            for (int i = 0; i < n; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(CoopState.LobbyId, i);
                if (member == CSteamID.Nil || member == myId) continue;

                SteamNetworking.AcceptP2PSessionWithUser(member);

                if (CoopState.Peers.Contains(member)) continue;

                CoopState.Peers.Add(member);
                _lastHeartbeat[member] = Time.time;
                CoopState.IsConnected  = true;
                Plugin.Log.LogInfo(
                    $"[LobbyManager] [Host] 로비 스캔: Guest 등록 {SteamFriends.GetFriendPersonaName(member)} ({member})");

                var myIdLocal = SteamUser.GetSteamID();
                _transport.SendReliable(member,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myIdLocal, isHost: true));
                SendFullSnapshotTo(member);
                TweakSyncHandler.SendTo(member);

                var scene = MissionState.HostCurrentScene;
                var idx   = MissionState.HostCurrentBuildIndex;
                if (!string.IsNullOrEmpty(scene) && idx > 0)
                {
                    _transport.SendReliable(member, MissionStartPacket.Encode(scene, idx));
                    Plugin.Log.LogInfo($"[LobbyManager] MissionStart 재전송 → {member}");
                }
            }
        }

        private void OnP2PConnectFail(P2PSessionConnectFail_t parm)
        {
            Plugin.Log.LogWarning(
                $"[LobbyManager] P2P 연결 실패: {parm.m_steamIDRemote} (오류={parm.m_eP2PSessionError})");
            RemovePeer(parm.m_steamIDRemote);
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t parm)
        {
            Plugin.Log.LogInfo($"[LobbyManager] 친구 초대로 로비 참가: {parm.m_steamIDLobby}");
            JoinLobby(parm.m_steamIDLobby);
        }

        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Plugin.Log.LogError("[LobbyManager] 로비 생성 실패");
                return;
            }
            CoopState.IsEnabled   = true;
            CoopState.IsHost      = true;
            CoopState.IsConnected = false;
            CoopState.LobbyId     = new CSteamID(result.m_ulSteamIDLobby);
            CoopState.InitAsHost();

            var active = SceneManager.GetActiveScene();
            if (active.buildIndex > 0 && !active.name.StartsWith("00_"))
            {
                MissionState.HostCurrentScene      = active.name;
                MissionState.HostCurrentBuildIndex = active.buildIndex;
                Plugin.Log.LogInfo($"[LobbyManager] 로비 생성 시 현재 씬 기록: '{active.name}'");
            }

            Plugin.Log.LogInfo($"[LobbyManager] 로비 생성 완료: {CoopState.LobbyId}");
        }

        private void OnLobbyEnter(LobbyEnter_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                Plugin.Log.LogError("[LobbyManager] 로비 입장 실패 (IO)");
                return;
            }
            if (result.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Plugin.Log.LogError($"[LobbyManager] 로비 입장 실패: {result.m_EChatRoomEnterResponse}");
                return;
            }

            CoopState.IsEnabled = true;
            CoopState.IsHost    = false;
            CoopState.LobbyId   = new CSteamID(result.m_ulSteamIDLobby);
            CoopState.InitAsGuest();

            var hostId = SteamMatchmaking.GetLobbyOwner(CoopState.LobbyId);
            var myId   = SteamUser.GetSteamID();

            Plugin.Log.LogInfo(
                $"[LobbyManager] 로비 입장: lobbyId={CoopState.LobbyId} hostId={hostId} myId={myId}");

            if (hostId != CSteamID.Nil && hostId != myId)
                ConnectToHost(hostId, myId);
            else
            {
                Plugin.Log.LogWarning(
                    $"[LobbyManager] hostId 미확정 (hostId={hostId}) — LobbyDataUpdate 대기");
                SteamMatchmaking.RequestLobbyData(CoopState.LobbyId);
            }
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t parm)
        {
            if (!CoopState.IsEnabled || CoopState.IsHost || CoopState.IsConnected) return;
            if (parm.m_ulSteamIDLobby != (ulong)CoopState.LobbyId) return;

            var hostId = SteamMatchmaking.GetLobbyOwner(CoopState.LobbyId);
            var myId   = SteamUser.GetSteamID();

            Plugin.Log.LogInfo(
                $"[LobbyManager] LobbyDataUpdate 수신: lobbyId={parm.m_ulSteamIDLobby} hostId={hostId}");

            if (hostId != CSteamID.Nil && hostId != myId)
                ConnectToHost(hostId, myId);
            else
                Plugin.Log.LogWarning(
                    $"[LobbyManager] LobbyDataUpdate 후에도 hostId 미확정: {hostId}");
        }

        public void ConnectToHost(CSteamID hostId, CSteamID myId)
        {
            if (!CoopState.Peers.Contains(hostId))
            {
                CoopState.Peers.Add(hostId);
                _lastHeartbeat[hostId] = Time.time;
            }
            SteamNetworking.AcceptP2PSessionWithUser(hostId);
            CoopState.IsConnected = true;
            _transport.SendReliable(hostId,
                HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: false));
            Plugin.Log.LogInfo($"[LobbyManager] Host에게 Handshake 전송 완료 → {hostId}");
        }

        private void RetryConnectToHost()
        {
            var myId   = SteamUser.GetSteamID();
            var hostId = SteamMatchmaking.GetLobbyOwner(CoopState.LobbyId);

            if (hostId == CSteamID.Nil || hostId == myId)
            {
                int n = SteamMatchmaking.GetNumLobbyMembers(CoopState.LobbyId);
                for (int i = 0; i < n; i++)
                {
                    var m = SteamMatchmaking.GetLobbyMemberByIndex(CoopState.LobbyId, i);
                    if (m != myId && m != CSteamID.Nil) { hostId = m; break; }
                }
            }

            if (hostId == CSteamID.Nil || hostId == myId)
            {
                Plugin.Log.LogWarning(
                    $"[LobbyManager] 재시도: Host 미발견 (멤버={SteamMatchmaking.GetNumLobbyMembers(CoopState.LobbyId)})");
                return;
            }

            Plugin.Log.LogInfo($"[LobbyManager] 재시도: Handshake → {hostId}");
            ConnectToHost(hostId, myId);
        }

        private void CheckTimeouts(float now)
        {
            var toRemove = new System.Collections.Generic.List<CSteamID>();
            foreach (var kv in _lastHeartbeat)
            {
                if (now - kv.Value > HeartbeatTimeout)
                {
                    Plugin.Log.LogWarning($"[LobbyManager] Heartbeat 타임아웃: {kv.Key}");
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var peer in toRemove)
                RemovePeer(peer);
        }

        public void RemovePeer(CSteamID peer)
        {
            CoopState.Peers.Remove(peer);
            _lastHeartbeat.Remove(peer);
            SteamNetworking.CloseP2PSessionWithUser(peer);
            PlayerSyncHandler.RemoveBot((ulong)peer);

            if (CoopState.Peers.Count == 0)
            {
                CoopState.IsConnected = false;
                Plugin.Log.LogInfo("[LobbyManager] 모든 Peer 연결 끊김");

                // Guest가 미션 씬에서 Host와 연결이 끊긴 경우 MetaGame으로 복귀
                if (!CoopState.IsHost)
                {
                    var scene    = SceneManager.GetActiveScene();
                    bool inMission = scene.buildIndex > 0
                                  && !scene.name.StartsWith("00_")
                                  && scene.name != Mission.MissionState.MetaGameScene;
                    if (inMission)
                    {
                        Plugin.Log.LogInfo("[LobbyManager] Host 연결 끊김 (미션 중) — MetaGame으로 복귀");
                        Mission.MissionState.Status       = Mission.MissionStatus.Idle;
                        Mission.MissionState.SessionState = Mission.CoopSessionState.InLobby;
                        SceneManager.LoadScene(Mission.MissionState.MetaGameScene);
                    }
                }
            }
        }

        private void SendFullSnapshotTo(CSteamID guest)
        {
            var scanner = Ecs.EntityScanner.Instance;
            if (scanner == null) return;

            var snapshot = scanner.GetCurrentSnapshot();
            var pkt = Packet.Encode(PacketType.FullSnapshot, w =>
            {
                w.Write(snapshot.Count);
                foreach (var (_, snap) in snapshot)
                {
                    w.Write(snap.HostEntityIndex);
                    w.Write((ushort)0);
                    w.Write(snap.PosX); w.Write(snap.PosY); w.Write(snap.PosZ);
                    w.Write(snap.Hp);
                }
            });
            _transport.SendReliable(guest, pkt);
            Plugin.Log.LogInfo($"[LobbyManager] FullSnapshot 전송: {snapshot.Count} 에너미 → {guest}");
        }
    }
}
