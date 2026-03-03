using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Events;
using BloodshedModToolkit.Coop.Ecs;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.UI;

namespace BloodshedModToolkit.Coop.Net
{
    public class NetManager : MonoBehaviour
    {
        public NetManager(IntPtr ptr) : base(ptr) { }

        // ── 싱글톤 ───────────────────────────────────────────────────────────
        public static NetManager? Instance { get; private set; }

        // ── 라우터 ───────────────────────────────────────────────────────────
        public PacketRouter Router { get; } = new();

        // ── Steam 콜백 ───────────────────────────────────────────────────────
        private Callback<P2PSessionRequest_t>?      _cbP2PRequest;
        private Callback<P2PSessionConnectFail_t>?  _cbP2PFail;
        private Callback<GameLobbyJoinRequested_t>? _cbJoinRequested;
        private Callback<LobbyDataUpdate_t>?        _cbLobbyDataUpdate;
        private CallResult<LobbyCreated_t>?         _crLobbyCreated;
        private CallResult<LobbyEnter_t>?           _crLobbyEnter;

        // ── 수신 버퍼 ────────────────────────────────────────────────────────
        private const int MaxPacketSize = 4096;
        private readonly byte[] _recvBuf = new byte[MaxPacketSize];

        // ── Heartbeat ────────────────────────────────────────────────────────
        private float _heartbeatTimer;
        private const float HeartbeatInterval = 30f;
        private const float HeartbeatTimeout  = 60f;
        private readonly Dictionary<CSteamID, float> _lastHeartbeat = new();

        // ── Guest 연결 재시도 ────────────────────────────────────────────────
        private float _retryTimer;
        private const float RetryInterval = 5f;

        // ── Host 로비 멤버 스캔 ──────────────────────────────────────────────
        // P2PSessionRequest_t.m_steamIDRemote가 IL2CPP 레이아웃 불일치로
        // 잘못된 SteamID를 반환하므로, 로비 멤버 목록에서 직접 ID를 가져와 처리
        private float _memberScanTimer;
        private const float MemberScanInterval = 2f;

        // ════════════════════════════════════════════════════════════════════
        // Unity 생명주기
        // ════════════════════════════════════════════════════════════════════
        void Awake() => Instance = this;

        void Start()
        {
            // Action<T> → DispatchDelegate 암묵적 변환 (Il2CppInterop 생성 연산자)
            Action<P2PSessionRequest_t>      onReq        = OnP2PSessionRequest;
            Action<P2PSessionConnectFail_t>  onFail       = OnP2PConnectFail;
            Action<GameLobbyJoinRequested_t> onJoin       = OnGameLobbyJoinRequested;
            Action<LobbyDataUpdate_t>        onLobbyData  = OnLobbyDataUpdate;

            _cbP2PRequest      = Callback<P2PSessionRequest_t>.Create(onReq);
            _cbP2PFail         = Callback<P2PSessionConnectFail_t>.Create(onFail);
            _cbJoinRequested   = Callback<GameLobbyJoinRequested_t>.Create(onJoin);
            _cbLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(onLobbyData);

            Router.Register(PacketType.Handshake,     HandleHandshake);
            Router.Register(PacketType.Heartbeat,     HandleHeartbeat);
            Router.Register(PacketType.EntitySpawn,   HandleEntitySpawn);
            Router.Register(PacketType.EntityDespawn, HandleEntityDespawn);
            Router.Register(PacketType.WaveAdvance,   HandleWaveAdvance);
            Router.Register(PacketType.XpGained,      HandleXpGained);
            Router.Register(PacketType.LevelUp,       HandleLevelUp);
            Router.Register(PacketType.PlayerState,    HandlePlayerState);
            Router.Register(PacketType.StateSnapshot,  HandleStateSnapshot);
            Router.Register(PacketType.FullSnapshot,   HandleFullSnapshot);
            Router.Register(PacketType.DamageRequest,  HandleDamageRequest);
            Router.Register(PacketType.TweakSync,      HandleTweakSync);
            Router.Register(PacketType.ItemSelected,   HandleItemSelected);
            Router.Register(PacketType.MissionStart,   HandleMissionStart);
            Router.Register(PacketType.PlayerReady,    HandlePlayerReady);
            Router.Register(PacketType.ChatMessage,    HandleChatMessage);
            Router.Register(PacketType.VoteStart,      HandleVoteStart);
            Router.Register(PacketType.VoteAccept,     HandleVoteAccept);

            Plugin.Log.LogInfo("[NetManager] 초기화 완료");
        }

        void OnDestroy()
        {
            LeaveLobby();
            _cbP2PRequest?.Dispose();
            _cbP2PFail?.Dispose();
            _cbJoinRequested?.Dispose();
            _cbLobbyDataUpdate?.Dispose();
            _crLobbyCreated?.Dispose();
            _crLobbyEnter?.Dispose();
            Instance = null;
            Plugin.Log.LogInfo("[NetManager] 종료");
        }

        void Update()
        {
            if (!CoopState.IsEnabled) return;

            PollMessages(0);  // 채널 0 — Reliable (이벤트)
            PollMessages(1);  // 채널 1 — Unreliable (위치 스냅샷)

            // Host: 로비 멤버 목록을 주기적으로 스캔 — P2PSessionRequest_t의
            // SteamID가 IL2CPP 불일치로 잘못될 수 있으므로 이 경로가 실제 연결을 담당
            if (CoopState.IsHost && CoopState.LobbyId != CSteamID.Nil)
            {
                _memberScanTimer += Time.deltaTime;
                if (_memberScanTimer >= MemberScanInterval)
                {
                    _memberScanTimer = 0f;
                    ScanAndRegisterLobbyMembers();
                }
            }

            // Guest: 미연결 상태면 주기적으로 Host에게 Handshake 재전송
            if (!CoopState.IsHost && !CoopState.IsConnected && CoopState.LobbyId != CSteamID.Nil)
            {
                _retryTimer += Time.deltaTime;
                if (_retryTimer >= RetryInterval)
                {
                    _retryTimer = 0f;
                    RetryConnectToHost();
                }
            }

            if (CoopState.IsConnected)
            {
                _heartbeatTimer += Time.deltaTime;
                if (_heartbeatTimer >= HeartbeatInterval)
                {
                    _heartbeatTimer = 0f;
                    BroadcastReliable(HeartbeatPacket.Encode());
                }
                CheckTimeouts();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 로비 관리
        // ════════════════════════════════════════════════════════════════════
        public void CreateLobby(int maxPlayers = 4)
        {
            Plugin.Log.LogInfo($"[NetManager] 로비 생성 (최대 {maxPlayers}인)...");
            Action<LobbyCreated_t, bool> onCreated = OnLobbyCreated;
            _crLobbyCreated = CallResult<LobbyCreated_t>.Create(onCreated);
            var handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
            _crLobbyCreated.Set(handle);
        }

        public void JoinLobby(CSteamID lobbyId)
        {
            Plugin.Log.LogInfo($"[NetManager] 로비 참가: {lobbyId}");
            Action<LobbyEnter_t, bool> onEnter = OnLobbyEnter;
            _crLobbyEnter = CallResult<LobbyEnter_t>.Create(onEnter);
            var handle = SteamMatchmaking.JoinLobby(lobbyId);
            _crLobbyEnter.Set(handle);
        }

        public void LeaveLobby()
        {
            if (!CoopState.IsEnabled) return;
            // 게임 종료 시 Steam API가 먼저 해제될 수 있으므로 예외 보호
            try
            {
                foreach (var peer in CoopState.Peers)
                    SteamNetworking.CloseP2PSessionWithUser(peer);
                if (CoopState.LobbyId != CSteamID.Nil)
                    SteamMatchmaking.LeaveLobby(CoopState.LobbyId);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"[NetManager] LeaveLobby Steam 예외 (종료 중?): {e.Message}");
            }
            CoopState.Reset();
            PlayerSyncHandler.Reset();
            _lastHeartbeat.Clear();
            _heartbeatTimer = 0f;
            Plugin.Log.LogInfo("[NetManager] 로비 퇴장");
        }

        // ════════════════════════════════════════════════════════════════════
        // 패킷 송신
        // ════════════════════════════════════════════════════════════════════
        public void SendReliable(CSteamID peer, byte[] data)
            => SteamNetworking.SendP2PPacket(peer, data, (uint)data.Length,
                EP2PSend.k_EP2PSendReliable, 0);

        public void SendUnreliable(CSteamID peer, byte[] data)
            => SteamNetworking.SendP2PPacket(peer, data, (uint)data.Length,
                EP2PSend.k_EP2PSendUnreliable, 1);

        public void BroadcastReliable(byte[] data)
        {
            foreach (var peer in CoopState.Peers)
                SendReliable(peer, data);
        }

        public void BroadcastUnreliable(byte[] data)
        {
            foreach (var peer in CoopState.Peers)
                SendUnreliable(peer, data);
        }

        /// <summary>Phase 7: EntityScanner 주기적 FullSnapshot 트리거에서 호출.</summary>
        public void BroadcastFullSnapshot()
        {
            foreach (var peer in CoopState.Peers)
                SendFullSnapshotTo(peer);
        }

        // ════════════════════════════════════════════════════════════════════
        // 패킷 수신 폴링
        // ════════════════════════════════════════════════════════════════════
        private void PollMessages(int channel)
        {
            while (SteamNetworking.IsP2PPacketAvailable(out var size, channel))
            {
                Plugin.Log.LogDebug($"[NetManager] 수신 대기 패킷: ch={channel} size={size}");

                if (size > MaxPacketSize)
                {
                    Plugin.Log.LogWarning($"[NetManager] 패킷 크기 초과: {size} > {MaxPacketSize}");
                    SteamNetworking.ReadP2PPacket(_recvBuf, (uint)_recvBuf.Length,
                        out _, out _, channel);
                    continue;
                }

                bool ok = SteamNetworking.ReadP2PPacket(_recvBuf, size,
                    out uint read, out CSteamID from, channel);
                Plugin.Log.LogDebug($"[NetManager] ReadP2PPacket: ok={ok} read={read} from={from}");

                if (ok && read > 0)
                {
                    var data = new byte[read];
                    Buffer.BlockCopy(_recvBuf, 0, data, 0, (int)read);
                    if (CoopState.Peers.Contains(from))
                        _lastHeartbeat[from] = Time.time;
                    Router.Dispatch(from, data);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Steam 콜백 핸들러
        // ════════════════════════════════════════════════════════════════════
        private void OnP2PSessionRequest(P2PSessionRequest_t parm)
        {
            // P2PSessionRequest_t.m_steamIDRemote가 IL2CPP struct 레이아웃 불일치로
            // 잘못된 SteamID를 반환할 수 있음. AcceptP2PSessionWithUser는 호출하되,
            // 실제 Peer 등록은 ScanAndRegisterLobbyMembers()에서 올바른 SteamID로 처리
            Plugin.Log.LogInfo($"[NetManager] P2P 세션 요청 수락: {parm.m_steamIDRemote}");
            SteamNetworking.AcceptP2PSessionWithUser(parm.m_steamIDRemote);
        }

        /// <summary>
        /// Host: 로비 멤버 목록에서 직접 SteamID를 가져와 세션 수락 + Peer 등록.
        /// P2PSessionRequest_t의 SteamID 불일치 문제를 우회.
        /// </summary>
        private void ScanAndRegisterLobbyMembers()
        {
            var myId = SteamUser.GetSteamID();
            int n    = SteamMatchmaking.GetNumLobbyMembers(CoopState.LobbyId);

            for (int i = 0; i < n; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(CoopState.LobbyId, i);
                if (member == CSteamID.Nil || member == myId) continue;

                // 세션 명시적 수락 (P2PSessionRequest 콜백보다 신뢰도 높음)
                SteamNetworking.AcceptP2PSessionWithUser(member);

                if (CoopState.Peers.Contains(member)) continue;

                // 신규 Guest — Peer 등록 + 초기화 패킷 전송
                CoopState.Peers.Add(member);
                _lastHeartbeat[member] = Time.time;
                CoopState.IsConnected  = true;
                Plugin.Log.LogInfo(
                    $"[NetManager] [Host] 로비 스캔: Guest 등록 {SteamFriends.GetFriendPersonaName(member)} ({member})");

                SendReliable(member,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: true));
                SendFullSnapshotTo(member);
                TweakSyncHandler.SendTo(member);

                var scene = Mission.MissionState.HostCurrentScene;
                var idx   = Mission.MissionState.HostCurrentBuildIndex;
                if (!string.IsNullOrEmpty(scene) && idx > 0)
                {
                    SendReliable(member, Net.MissionStartPacket.Encode(scene, idx));
                    Plugin.Log.LogInfo($"[NetManager] MissionStart 재전송 → {member}");
                }
            }
        }

        private void OnP2PConnectFail(P2PSessionConnectFail_t parm)
        {
            Plugin.Log.LogWarning(
                $"[NetManager] P2P 연결 실패: {parm.m_steamIDRemote} (오류={parm.m_eP2PSessionError})");
            RemovePeer(parm.m_steamIDRemote);
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t parm)
        {
            Plugin.Log.LogInfo($"[NetManager] 친구 초대로 로비 참가: {parm.m_steamIDLobby}");
            JoinLobby(parm.m_steamIDLobby);
        }

        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Plugin.Log.LogError("[NetManager] 로비 생성 실패");
                return;
            }
            CoopState.IsEnabled   = true;
            CoopState.IsHost      = true;
            CoopState.IsConnected = false;
            CoopState.LobbyId     = new CSteamID(result.m_ulSteamIDLobby);
            Plugin.Log.LogInfo($"[NetManager] 로비 생성 완료: {CoopState.LobbyId}");
        }

        private void OnLobbyEnter(LobbyEnter_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                Plugin.Log.LogError("[NetManager] 로비 입장 실패 (IO)");
                return;
            }
            // m_EChatRoomEnterResponse is uint in Steamworks.NET
            if (result.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Plugin.Log.LogError($"[NetManager] 로비 입장 실패: {result.m_EChatRoomEnterResponse}");
                return;
            }

            CoopState.IsEnabled = true;
            CoopState.IsHost    = false;
            CoopState.LobbyId   = new CSteamID(result.m_ulSteamIDLobby);

            var hostId = SteamMatchmaking.GetLobbyOwner(CoopState.LobbyId);
            var myId   = SteamUser.GetSteamID();

            Plugin.Log.LogInfo(
                $"[NetManager] 로비 입장: lobbyId={CoopState.LobbyId} hostId={hostId} myId={myId}");

            if (hostId != CSteamID.Nil && hostId != myId)
            {
                ConnectToHost(hostId, myId);
            }
            else
            {
                Plugin.Log.LogWarning(
                    $"[NetManager] hostId 미확정 (hostId={hostId}) — LobbyDataUpdate 대기");
                SteamMatchmaking.RequestLobbyData(CoopState.LobbyId);
            }
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t parm)
        {
            // 이미 연결됐거나 Host 쪽이면 무시
            if (!CoopState.IsEnabled || CoopState.IsHost || CoopState.IsConnected) return;
            if (parm.m_ulSteamIDLobby != (ulong)CoopState.LobbyId) return;

            var hostId = SteamMatchmaking.GetLobbyOwner(CoopState.LobbyId);
            var myId   = SteamUser.GetSteamID();

            Plugin.Log.LogInfo(
                $"[NetManager] LobbyDataUpdate 수신: lobbyId={parm.m_ulSteamIDLobby} hostId={hostId}");

            if (hostId != CSteamID.Nil && hostId != myId)
                ConnectToHost(hostId, myId);
            else
                Plugin.Log.LogWarning(
                    $"[NetManager] LobbyDataUpdate 후에도 hostId 미확정: {hostId}");
        }

        private void ConnectToHost(CSteamID hostId, CSteamID myId)
        {
            if (!CoopState.Peers.Contains(hostId))
            {
                CoopState.Peers.Add(hostId);
                _lastHeartbeat[hostId] = Time.time;
            }
            CoopState.IsConnected = true;
            SendReliable(hostId,
                HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: false));
            Plugin.Log.LogInfo($"[NetManager] Host에게 Handshake 전송 완료 → {hostId}");
        }

        private void RetryConnectToHost()
        {
            var myId   = SteamUser.GetSteamID();
            var hostId = SteamMatchmaking.GetLobbyOwner(CoopState.LobbyId);

            // GetLobbyOwner 실패 시 멤버 목록에서 자신이 아닌 첫 번째 멤버를 Host로 간주
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
                    $"[NetManager] 재시도: Host 미발견 (멤버={SteamMatchmaking.GetNumLobbyMembers(CoopState.LobbyId)})");
                return;
            }

            Plugin.Log.LogInfo($"[NetManager] 재시도: Handshake → {hostId}");
            ConnectToHost(hostId, myId);
        }

        // ════════════════════════════════════════════════════════════════════
        // 내부 패킷 핸들러
        // ════════════════════════════════════════════════════════════════════
        private void HandleHandshake(CSteamID from, byte[] payload)
        {
            Plugin.Log.LogInfo($"[NetManager] Handshake 수신: {from}");
            var pkt = HandshakePacket.Decode(payload);

            if (pkt.Version != CoopState.CoopVersion)
            {
                Plugin.Log.LogWarning(
                    $"[NetManager] 버전 불일치: {pkt.Version} ≠ {CoopState.CoopVersion}");
                SteamNetworking.CloseP2PSessionWithUser(from);
                return;
            }

            if (!CoopState.Peers.Contains(from))
            {
                CoopState.Peers.Add(from);
                _lastHeartbeat[from] = Time.time;
                Plugin.Log.LogInfo(
                    $"[NetManager] 새 Peer 등록: {SteamFriends.GetFriendPersonaName(from)} ({from})");
            }

            // Handshake 수신 시 항상 연결 확립 (이미 Peers에 있어도 IsConnected가 false일 수 있음)
            if (!CoopState.IsConnected)
            {
                CoopState.IsConnected = true;
                Plugin.Log.LogInfo($"[NetManager] 연결 확립 ← {from}");
            }

            // Host이면 응답 Handshake + FullSnapshot + TweakConfig 전송
            // (OnP2PSessionRequest에서 즉시 처리되지 않은 경우의 fallback)
            if (CoopState.IsHost)
            {
                var myId = SteamUser.GetSteamID();
                SendReliable(from,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: true));
                SendFullSnapshotTo(from);
                TweakSyncHandler.SendTo(from);

                var scene = Mission.MissionState.HostCurrentScene;
                var idx   = Mission.MissionState.HostCurrentBuildIndex;
                if (!string.IsNullOrEmpty(scene) && idx > 0)
                {
                    SendReliable(from, Net.MissionStartPacket.Encode(scene, idx));
                    Plugin.Log.LogInfo(
                        $"[NetManager] MissionStart 재전송 (Handshake fallback): '{scene}' → {from}");
                }
            }
        }

        private void SendFullSnapshotTo(CSteamID guest)
        {
            var scanner = EntityScanner.Instance;
            if (scanner == null) return;

            var snapshot = scanner.GetCurrentSnapshot();
            var pkt = Packet.Encode(PacketType.FullSnapshot, w =>
            {
                w.Write(snapshot.Count);
                foreach (var (_, snap) in snapshot)
                {
                    w.Write(snap.HostEntityIndex);
                    w.Write((ushort)0);  // typeId — Phase 5에서 정밀화
                    w.Write(snap.PosX); w.Write(snap.PosY); w.Write(snap.PosZ);
                    w.Write(snap.Hp);
                }
            });
            SendReliable(guest, pkt);
            Plugin.Log.LogInfo($"[NetManager] FullSnapshot 전송: {snapshot.Count} 에너미 → {guest}");
        }

        private void HandleHeartbeat(CSteamID from, byte[] payload)
        {
            _lastHeartbeat[from] = Time.time;
        }

        // ════════════════════════════════════════════════════════════════════
        // Heartbeat 타임아웃 체크
        // ════════════════════════════════════════════════════════════════════
        private void CheckTimeouts()
        {
            float now      = Time.time;
            var   toRemove = new List<CSteamID>();

            foreach (var kv in _lastHeartbeat)
            {
                if (now - kv.Value > HeartbeatTimeout)
                {
                    Plugin.Log.LogWarning($"[NetManager] Heartbeat 타임아웃: {kv.Key}");
                    toRemove.Add(kv.Key);
                }
            }

            foreach (var peer in toRemove)
                RemovePeer(peer);
        }

        private void RemovePeer(CSteamID peer)
        {
            CoopState.Peers.Remove(peer);
            _lastHeartbeat.Remove(peer);
            SteamNetworking.CloseP2PSessionWithUser(peer);
            PlayerSyncHandler.RemoveBot((ulong)peer);

            if (CoopState.Peers.Count == 0)
            {
                CoopState.IsConnected = false;
                Plugin.Log.LogInfo("[NetManager] 모든 Peer 연결 끊김");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Phase 2 패킷 핸들러
        // ════════════════════════════════════════════════════════════════════
        private void HandleEntitySpawn(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = EntitySpawnPacket.Decode(payload);
            // SpawnEventPatch.Postfix (Guest 경로)가 실행될 때 순서 매핑
            EntityRegistry.PendingHostIds.Enqueue(pkt.HostEntityIndex);
            Plugin.Log.LogDebug(
                $"[NetManager] EntitySpawn 큐: idx={pkt.HostEntityIndex} type={pkt.EnemyTypeId}");
        }

        private void HandleEntityDespawn(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = EntityDespawnPacket.Decode(payload);
            // Phase 5: Health 캐시도 정리
            if (EntityRegistry.HostToLocal.TryGetLocal(pkt.HostEntityIndex, out int localId))
                EntityRegistry.LocalHealth.Remove(localId);
            EntityRegistry.HostToLocal.Remove(pkt.HostEntityIndex);
            Plugin.Log.LogDebug($"[NetManager] EntityDespawn: idx={pkt.HostEntityIndex}");
        }

        private void HandleWaveAdvance(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = WaveAdvancePacket.Decode(payload);
            var sp  = UnityEngine.Object.FindObjectOfType<SpawnProcessor>();
            if (sp == null) return;

            sp.currentWaveIndex = pkt.WaveIndex;
            WaveGroupStartPatch._allowGuestTrigger = true;
            try   { sp.NextWave(); }
            finally { WaveGroupStartPatch._allowGuestTrigger = false; }
            Plugin.Log.LogInfo($"[NetManager] WaveAdvance: wave={pkt.WaveIndex}");
        }

        private void HandleXpGained(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = XpGainedPacket.Decode(payload);
            var ps  = UnityEngine.Object.FindObjectOfType<PlayerStats>();
            if (ps == null) return;

            XpEventPatch._applyingRemoteXp = true;
            try   { ps.AddXp(pkt.Amount); }
            finally { XpEventPatch._applyingRemoteXp = false; }
        }

        private void HandleLevelUp(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = LevelUpPacket.Decode(payload);
            XpSyncHandler.ApplyLevelUp(pkt.NewLevel);
        }

        private void HandlePlayerState(CSteamID from, byte[] payload)
        {
            var pkt = PlayerStatePacket.Decode(payload);
            PlayerSyncHandler.OnPlayerState(pkt.SteamId, pkt);
        }

        private void HandleStateSnapshot(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var applicator = StateApplicator.Instance;

            using var ms = new System.IO.MemoryStream(payload);
            using var br = new System.IO.BinaryReader(ms);
            uint   tick  = br.ReadUInt32();
            ushort count = br.ReadUInt16();
            for (int i = 0; i < count; i++)
            {
                uint   hostIdx = br.ReadUInt32();
                float  px = br.ReadSingle(), py = br.ReadSingle(), pz = br.ReadSingle();
                ushort hp = br.ReadUInt16();
                applicator?.EnqueueUpdate(hostIdx, px, py, pz, hp);
            }
        }

        private void HandleTweakSync(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;   // Guest만 수신
            var pkt = TweakSyncPacket.Decode(payload);
            TweakSyncHandler.ApplyFromPacket(pkt);
        }

        private void HandleItemSelected(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = ItemSelectedPacket.Decode(payload);
            // TODO: 게임 아이템 선택 API 확인 후 자동 적용 구현
            Plugin.Log.LogInfo(
                $"[NetManager] ItemSelected 수신: index={pkt.ItemIndex}  (자동 적용 미구현)");
        }

        private void HandleMissionStart(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;  // Guest만 수신
            var (sceneName, buildIndex) = MissionStartPacket.Decode(payload);
            MissionSyncHandler.OnMissionStart(sceneName, buildIndex);
        }

        private void HandlePlayerReady(CSteamID from, byte[] payload)
        {
            if (!CoopState.IsHost) return;  // Host만 처리
            MissionSyncHandler.OnPlayerReady((ulong)from);
        }

        private void HandleChatMessage(CSteamID from, byte[] payload)
        {
            var (senderName, message) = ChatMessagePacket.Decode(payload);
            UI.ChatWindow.Instance?.AddMessage(senderName, message);
        }

        private void HandleVoteStart(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;  // Guest만 수신
            Sync.MissionSyncHandler.OnVoteStart();
        }

        private void HandleVoteAccept(CSteamID from, byte[] payload)
        {
            if (!CoopState.IsHost) return;  // Host만 처리
            bool accepted = VoteAcceptPacket.Decode(payload);
            Sync.MissionSyncHandler.OnVoteAccept((ulong)from, accepted);
        }

        private void HandleDamageRequest(CSteamID from, byte[] payload)
        {
            if (!CoopState.IsHost) return;
            var pkt = DamageRequestPacket.Decode(payload);

            // Host 측: hostIdx == GetInstanceID() (uint → int)
            int localId = (int)pkt.HostEntityIndex;
            var health  = FindHealthById(localId);
            if (health == null || health.isPlayer) return;

            health.Damage(pkt.Damage, null!, 0f, 0f, default, default, true);
            Plugin.Log.LogDebug(
                $"[NetManager] DamageRequest 적용: idx={pkt.HostEntityIndex} dmg={pkt.Damage:F1}");
        }

        /// <summary>
        /// localId(GetInstanceID) 로 Health 컴포넌트를 찾아 LocalHealth 캐시에 등록.
        /// EnemyIdentityCard.gameObject 가 interop에서 노출되지 않으므로
        /// FindObjectsOfType 으로 탐색 후 lazy-init 캐시를 채웁니다.
        /// </summary>
        private static Health? FindHealthById(int localId)
        {
            if (EntityRegistry.LocalHealth.TryGetValue(localId, out var cached) && cached != null)
                return cached;

            var all = UnityEngine.Object.FindObjectsOfType<Health>();
            if (all == null) return null;

            foreach (var h in all)
            {
                if (h.GetInstanceID() == localId)
                {
                    EntityRegistry.LocalHealth[localId] = h;
                    return h;
                }
            }
            return null;
        }

        private void HandleFullSnapshot(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            EntityRegistry.Reset();

            using var ms = new System.IO.MemoryStream(payload);
            using var br = new System.IO.BinaryReader(ms);
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                uint hostIdx = br.ReadUInt32();
                br.ReadUInt16();                      // typeId
                br.ReadSingle(); br.ReadSingle(); br.ReadSingle();  // pos
                br.ReadUInt16();                      // hp
                EntityRegistry.PendingHostIds.Enqueue(hostIdx);
            }
            Plugin.Log.LogInfo($"[NetManager] FullSnapshot 수신: {count} 에너미 큐잉");
        }
    }
}
