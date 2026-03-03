using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Events;
using BloodshedModToolkit.Coop.Ecs;
using BloodshedModToolkit.Coop.Sync;

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
            Router.Register(PacketType.PlayerState,    HandlePlayerState);
            Router.Register(PacketType.StateSnapshot,  HandleStateSnapshot);
            Router.Register(PacketType.FullSnapshot,   HandleFullSnapshot);
            Router.Register(PacketType.DamageRequest,  HandleDamageRequest);
            Router.Register(PacketType.TweakSync,      HandleTweakSync);
            Router.Register(PacketType.ItemSelected,   HandleItemSelected);

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

            // Host이면 응답 Handshake + FullSnapshot + TweakConfig 전송
            if (CoopState.IsHost)
            {
                var myId = SteamUser.GetSteamID();
                SendReliable(from,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: true));
                SendFullSnapshotTo(from);
                TweakSyncHandler.SendTo(from);
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
