using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Events;

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

        // ════════════════════════════════════════════════════════════════════
        // Unity 생명주기
        // ════════════════════════════════════════════════════════════════════
        void Awake() => Instance = this;

        void Start()
        {
            // Action<T> → DispatchDelegate 암묵적 변환 (Il2CppInterop 생성 연산자)
            Action<P2PSessionRequest_t>      onReq   = OnP2PSessionRequest;
            Action<P2PSessionConnectFail_t>  onFail  = OnP2PConnectFail;
            Action<GameLobbyJoinRequested_t> onJoin  = OnGameLobbyJoinRequested;

            _cbP2PRequest    = Callback<P2PSessionRequest_t>.Create(onReq);
            _cbP2PFail       = Callback<P2PSessionConnectFail_t>.Create(onFail);
            _cbJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(onJoin);

            Router.Register(PacketType.Handshake,     HandleHandshake);
            Router.Register(PacketType.Heartbeat,     HandleHeartbeat);
            Router.Register(PacketType.EntitySpawn,   HandleEntitySpawn);
            Router.Register(PacketType.EntityDespawn, HandleEntityDespawn);
            Router.Register(PacketType.WaveAdvance,   HandleWaveAdvance);
            Router.Register(PacketType.XpGained,      HandleXpGained);
            Router.Register(PacketType.LevelUp,       HandleLevelUp);
            Router.Register(PacketType.PlayerState,   HandlePlayerState);

            Plugin.Log.LogInfo("[NetManager] 초기화 완료");
        }

        void OnDestroy()
        {
            LeaveLobby();
            _cbP2PRequest?.Dispose();
            _cbP2PFail?.Dispose();
            _cbJoinRequested?.Dispose();
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
            foreach (var peer in CoopState.Peers)
                SteamNetworking.CloseP2PSessionWithUser(peer);
            if (CoopState.LobbyId != CSteamID.Nil)
                SteamMatchmaking.LeaveLobby(CoopState.LobbyId);
            CoopState.Reset();
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

        // ════════════════════════════════════════════════════════════════════
        // 패킷 수신 폴링
        // ════════════════════════════════════════════════════════════════════
        private void PollMessages(int channel)
        {
            while (SteamNetworking.IsP2PPacketAvailable(out var size, channel))
            {
                if (size > MaxPacketSize)
                {
                    Plugin.Log.LogWarning($"[NetManager] 패킷 크기 초과: {size} > {MaxPacketSize}");
                    SteamNetworking.ReadP2PPacket(_recvBuf, (uint)_recvBuf.Length,
                        out _, out _, channel);
                    continue;
                }

                if (SteamNetworking.ReadP2PPacket(_recvBuf, size,
                    out uint read, out CSteamID from, channel))
                {
                    var data = new byte[read];
                    Buffer.BlockCopy(_recvBuf, 0, data, 0, (int)read);
                    Router.Dispatch(from, data);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Steam 콜백 핸들러
        // ════════════════════════════════════════════════════════════════════
        private void OnP2PSessionRequest(P2PSessionRequest_t parm)
        {
            Plugin.Log.LogInfo($"[NetManager] P2P 세션 요청: {parm.m_steamIDRemote}");
            SteamNetworking.AcceptP2PSessionWithUser(parm.m_steamIDRemote);
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

            if (hostId != CSteamID.Nil && hostId != myId)
            {
                CoopState.Peers.Add(hostId);
                _lastHeartbeat[hostId] = Time.time;
                CoopState.IsConnected  = true;
                SendReliable(hostId,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: false));
                Plugin.Log.LogInfo($"[NetManager] 로비 입장 완료 → Host: {hostId}");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 내부 패킷 핸들러
        // ════════════════════════════════════════════════════════════════════
        private void HandleHandshake(CSteamID from, byte[] payload)
        {
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
                CoopState.IsConnected = true;
                Plugin.Log.LogInfo(
                    $"[NetManager] 새 Peer: {SteamFriends.GetFriendPersonaName(from)} ({from})");
            }

            // Host이면 응답 Handshake 전송
            if (CoopState.IsHost)
            {
                var myId = SteamUser.GetSteamID();
                SendReliable(from,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: true));
            }
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
            // Phase 4에서 실제 스폰 로직 구현
            Plugin.Log.LogInfo(
                $"[NetManager] EntitySpawn: idx={pkt.HostEntityIndex} type={pkt.EnemyTypeId}" +
                $" @ ({pkt.PosX:F1},{pkt.PosY:F1},{pkt.PosZ:F1})");
        }

        private void HandleEntityDespawn(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = EntityDespawnPacket.Decode(payload);
            // Phase 4에서 실제 디스폰 로직 구현
            Plugin.Log.LogInfo($"[NetManager] EntityDespawn: idx={pkt.HostEntityIndex}");
        }

        private void HandleWaveAdvance(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = WaveAdvancePacket.Decode(payload);
            var sp  = UnityEngine.Object.FindObjectOfType<SpawnProcessor>();
            if (sp == null) return;

            sp.currentWaveIndex = pkt.WaveIndex;
            WaveGroupStartPatch._allowGuestTrigger = true;
            sp.NextWave();
            WaveGroupStartPatch._allowGuestTrigger = false;
            Plugin.Log.LogInfo($"[NetManager] WaveAdvance: wave={pkt.WaveIndex}");
        }

        private void HandleXpGained(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = XpGainedPacket.Decode(payload);
            var ps  = UnityEngine.Object.FindObjectOfType<PlayerStats>();
            if (ps == null) return;

            XpEventPatch._applyingRemoteXp = true;
            ps.AddXp(pkt.Amount);
            XpEventPatch._applyingRemoteXp = false;
        }

        private void HandleLevelUp(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = LevelUpPacket.Decode(payload);
            // Phase 6에서 레벨업 아이템 선택 동기화 구현
            Plugin.Log.LogInfo($"[NetManager] LevelUp: level={pkt.NewLevel}");
        }

        private void HandlePlayerState(CSteamID from, byte[] payload)
        {
            var pkt = PlayerStatePacket.Decode(payload);
            // Phase 5에서 원격 플레이어 아바타 위치 업데이트 구현
            Plugin.Log.LogDebug(
                $"[NetManager] PlayerState from {from}: " +
                $"pos=({pkt.PosX:F1},{pkt.PosY:F1},{pkt.PosZ:F1}) hp={pkt.CurrentHp:F0}");
        }
    }
}
