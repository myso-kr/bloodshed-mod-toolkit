using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Events;
using BloodshedModToolkit.Coop.Ecs;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Renderer;
using BloodshedModToolkit.Coop.Bots;
using BloodshedModToolkit.UI;

namespace BloodshedModToolkit.Coop.Net
{
    public class NetManager : MonoBehaviour
    {
        public NetManager(IntPtr ptr) : base(ptr) { }

        public static NetManager? Instance { get; private set; }

        public PacketRouter Router { get; } = new();

        private readonly P2PTransport                _transport     = new();
        private readonly Dictionary<CSteamID, float> _lastHeartbeat = new();
        private LobbyManager? _lobby;

        void Awake() => Instance = this;

        void Start()
        {
            _lobby = new LobbyManager(_transport, Router, _lastHeartbeat);
            _lobby.Initialize();

            Router.Register(PacketType.Handshake,     HandleHandshake);
            Router.Register(PacketType.Heartbeat,     HandleHeartbeat);
            Router.Register(PacketType.EntitySpawn,   HandleEntitySpawn);
            Router.Register(PacketType.EntityDespawn, HandleEntityDespawn);
            Router.Register(PacketType.WaveAdvance,   HandleWaveAdvance);
            Router.Register(PacketType.XpGained,      HandleXpGained);
            Router.Register(PacketType.LevelUp,       HandleLevelUp);
            Router.Register(PacketType.PlayerState,   HandlePlayerState);
            Router.Register(PacketType.StateSnapshot, HandleStateSnapshot);
            Router.Register(PacketType.FullSnapshot,  HandleFullSnapshot);
            Router.Register(PacketType.DamageRequest, HandleDamageRequest);
            Router.Register(PacketType.TweakSync,     HandleTweakSync);
            Router.Register(PacketType.ItemSelected,  HandleItemSelected);
            Router.Register(PacketType.MissionStart,    HandleMissionStart);
            Router.Register(PacketType.PlayerReady,     HandlePlayerReady);
            Router.Register(PacketType.MissionBriefing, HandleMissionBriefing);
            Router.Register(PacketType.ChatMessage,     HandleChatMessage);
            Router.Register(PacketType.AttackEvent,     HandleAttackEvent);

            Plugin.Log.LogInfo("[NetManager] 초기화 완료");
        }

        void OnDestroy()
        {
            _lobby?.LeaveLobby();
            _lobby?.Dispose();
            Instance = null;
            Plugin.Log.LogInfo("[NetManager] 종료");
        }

        void Update()
        {
            if (!CoopState.IsEnabled) return;
            _transport.Poll(Router, from => _lastHeartbeat[from] = Time.time);
            _lobby?.Tick(Time.deltaTime, Time.time);
        }

        // ── 공개 API ──────────────────────────────────────────────────────────
        public void CreateLobby(int maxPlayers = 4) => _lobby?.CreateLobby(maxPlayers);
        public void JoinLobby(CSteamID id)          => _lobby?.JoinLobby(id);
        public void LeaveLobby()                    => _lobby?.LeaveLobby();

        public void SendReliable(CSteamID to, byte[] data)  => _transport.SendReliable(to, data);
        public void BroadcastReliable(byte[] data)          => _transport.BroadcastReliable(data);
        public void BroadcastUnreliable(byte[] data)        => _transport.BroadcastUnreliable(data);

        public void BroadcastFullSnapshot()
        {
            foreach (var peer in CoopState.Peers)
                SendFullSnapshotTo(peer);
        }

        // ── 패킷 핸들러 ───────────────────────────────────────────────────────
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

            if (!CoopState.IsConnected)
            {
                CoopState.IsConnected = true;
                Plugin.Log.LogInfo($"[NetManager] 연결 확립 ← {from}");
            }

            if (CoopState.IsHost)
            {
                var myId = SteamUser.GetSteamID();
                _transport.SendReliable(from,
                    HandshakePacket.Encode(CoopState.CoopVersion, (ulong)myId, isHost: true));
                SendFullSnapshotTo(from);
                TweakSyncHandler.SendTo(from);

                var scene = MissionState.HostCurrentScene;
                var idx   = MissionState.HostCurrentBuildIndex;
                if (!string.IsNullOrEmpty(scene) && idx > 0)
                {
                    _transport.SendReliable(from, MissionStartPacket.Encode(scene, idx));
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
                    w.Write((ushort)0);
                    w.Write(snap.PosX); w.Write(snap.PosY); w.Write(snap.PosZ);
                    w.Write(snap.Hp);
                }
            });
            _transport.SendReliable(guest, pkt);
            Plugin.Log.LogInfo($"[NetManager] FullSnapshot 전송: {snapshot.Count} 에너미 → {guest}");
        }

        private void HandleHeartbeat(CSteamID from, byte[] payload)
        {
            _lastHeartbeat[from] = Time.time;
        }

        private void HandleEntitySpawn(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = EntitySpawnPacket.Decode(payload);
            EntityRegistry.PendingHostIds.Enqueue(pkt.HostEntityIndex);
            Plugin.Log.LogDebug(
                $"[NetManager] EntitySpawn 큐: idx={pkt.HostEntityIndex} type={pkt.EnemyTypeId}");
        }

        private void HandleEntityDespawn(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = EntityDespawnPacket.Decode(payload);
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

            XpSyncHandler.WithRemoteXpGuard(() => ps.AddXp(pkt.Amount));
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
            if (CoopState.IsHost) return;
            var pkt = TweakSyncPacket.Decode(payload);
            TweakSyncHandler.ApplyFromPacket(pkt);
        }

        private void HandleItemSelected(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var pkt = ItemSelectedPacket.Decode(payload);
            Sync.ItemSyncHandler.ApplyItemSelection(pkt.ItemIndex);
        }

        private void HandleMissionStart(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var (sceneName, buildIndex) = MissionStartPacket.Decode(payload);
            CoopSessionManager.NotifyMissionStart(sceneName, buildIndex);
        }

        private void HandlePlayerReady(CSteamID from, byte[] payload)
        {
            if (!CoopState.IsHost) return;
            CoopSessionManager.NotifyGuestReady((ulong)from);
        }

        private void HandleMissionBriefing(CSteamID from, byte[] payload)
        {
            if (CoopState.IsHost) return;
            var (scene, idx) = MissionBriefingPacket.Decode(payload);
            CoopSessionManager.NotifyMissionBriefing(scene, idx);
        }

        private void HandleChatMessage(CSteamID from, byte[] payload)
        {
            var (senderName, message) = ChatMessagePacket.Decode(payload);
            UI.ChatWindow.Instance?.AddMessage(senderName, message);
        }

        private void HandleAttackEvent(CSteamID from, byte[] payload)
        {
            var pkt = AttackEventPacket.Decode(payload);

            // 아바타 공격 애니메이션
            if (Renderer.BotAvatarAnimator.Instances.TryGetValue(pkt.SteamId, out var anim))
                anim.TriggerAttack();

            // 무기 클래스별 공격 이펙트
            if (Sync.PlayerSyncHandler.States.TryGetValue(pkt.SteamId, out var state))
                Renderer.AttackEffectSpawner.Play(pkt.SteamId, (WeaponClass)(state.WeaponClassId % 4));
        }

        private void HandleDamageRequest(CSteamID from, byte[] payload)
        {
            if (!CoopState.IsHost) return;
            var pkt = DamageRequestPacket.Decode(payload);

            int localId = (int)pkt.HostEntityIndex;
            var health  = FindHealthById(localId);
            if (health == null || health.isPlayer) return;

            health.Damage(pkt.Damage, null!, 0f, 0f, default, default, true);
            Plugin.Log.LogDebug(
                $"[NetManager] DamageRequest 적용: idx={pkt.HostEntityIndex} dmg={pkt.Damage:F1}");
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
                br.ReadUInt16();
                br.ReadSingle(); br.ReadSingle(); br.ReadSingle();
                br.ReadUInt16();
                EntityRegistry.PendingHostIds.Enqueue(hostIdx);
            }
            Plugin.Log.LogInfo($"[NetManager] FullSnapshot 수신: {count} 에너미 큐잉");
        }

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
    }
}
