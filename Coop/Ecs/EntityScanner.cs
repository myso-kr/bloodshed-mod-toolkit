using System;
using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Ecs
{
    public class EntityScanner : MonoBehaviour
    {
        public EntityScanner(IntPtr ptr) : base(ptr) { }

        // ── 싱글톤 ────────────────────────────────────────────────────────────
        public static EntityScanner? Instance { get; private set; }

        // ── 설정 ─────────────────────────────────────────────────────────────
        private const float ScanInterval         = 0.05f;  // 20 Hz
        private const float FullSnapshotInterval = 60f;    // 1분마다 디싱크 복구
        private const int   MaxPerPacket         = 60;     // 60 × 18B = 1080B (MTU 안전)

        // ── 상태 ─────────────────────────────────────────────────────────────
        private float _scanTimer;
        private float _fullSnapshotTimer;
        private uint  _tick;

        private Dictionary<uint, EnemySnapshot> _lastSnapshot = new();

        // ── 스냅샷 구조체 ─────────────────────────────────────────────────────
        public struct EnemySnapshot
        {
            public uint   HostEntityIndex;
            public float  PosX, PosY, PosZ;
            public ushort Hp;
        }

        // ════════════════════════════════════════════════════════════════════
        // 생명주기
        // ════════════════════════════════════════════════════════════════════
        void Awake() => Instance = this;

        void OnDestroy() => Instance = null;

        /// <summary>현재 스냅샷 읽기 전용 뷰 (FullSnapshot 전송용).</summary>
        public IReadOnlyDictionary<uint, EnemySnapshot> GetCurrentSnapshot()
            => _lastSnapshot;

        void LateUpdate()
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;

            _scanTimer += Time.deltaTime;
            if (_scanTimer < ScanInterval) return;
            _scanTimer -= ScanInterval;

            var snapshots = ScanEnemies();
            var delta     = ComputeDelta(snapshots);

            if (delta.Count > 0)
                BroadcastSnapshot(delta);

            _lastSnapshot = snapshots;
            _tick++;

            // 주기적 FullSnapshot — 디싱크 복구 (60초마다 전체 상태 재전송)
            _fullSnapshotTimer += Time.deltaTime;
            if (_fullSnapshotTimer >= FullSnapshotInterval)
            {
                _fullSnapshotTimer = 0f;
                NetManager.Instance?.BroadcastFullSnapshot();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 스캔 — Health 캐시 기반 (SpawnEventPatch와 동일한 ID 공간)
        // ════════════════════════════════════════════════════════════════════
        private Dictionary<uint, EnemySnapshot> ScanEnemies()
        {
            var result = new Dictionary<uint, EnemySnapshot>();

            foreach (var kv in EntityRegistry.LocalHealth)
            {
                uint id     = (uint)kv.Key;
                var  health = kv.Value;
                if (health == null || health.isPlayer) continue;

                ushort hp = (ushort)Math.Clamp((int)health.currentHealth, 0, 65534);

                result[id] = new EnemySnapshot
                {
                    HostEntityIndex = id,
                    PosX = 0f, PosY = 0f, PosZ = 0f,
                    Hp   = hp,
                };
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        // 델타 압축
        // ════════════════════════════════════════════════════════════════════
        private List<EnemySnapshot> ComputeDelta(Dictionary<uint, EnemySnapshot> current)
        {
            var changed = new List<EnemySnapshot>();
            const ushort HpThreshold = 10;

            foreach (var (id, snap) in current)
            {
                if (!_lastSnapshot.TryGetValue(id, out var prev))
                {
                    changed.Add(snap);  // 신규 엔티티
                    continue;
                }

                if (Math.Abs(snap.Hp - prev.Hp) > HpThreshold)
                    changed.Add(snap);
            }
            return changed;
        }

        // ════════════════════════════════════════════════════════════════════
        // 브로드캐스트 (MTU 초과 시 청크 분할)
        // ════════════════════════════════════════════════════════════════════
        private void BroadcastSnapshot(List<EnemySnapshot> delta)
        {
            var net = NetManager.Instance;
            if (net == null) return;

            for (int start = 0; start < delta.Count; start += MaxPerPacket)
            {
                int  count = Math.Min(MaxPerPacket, delta.Count - start);
                uint tick  = _tick;
                int  s     = start;
                var  src   = delta;

                var pkt = Packet.Encode(PacketType.StateSnapshot, w =>
                {
                    w.Write(tick);
                    w.Write((ushort)count);
                    for (int j = s; j < s + count; j++)
                    {
                        var snap = src[j];
                        w.Write(snap.HostEntityIndex);
                        w.Write(snap.PosX);
                        w.Write(snap.PosY);
                        w.Write(snap.PosZ);
                        w.Write(snap.Hp);
                    }
                });

                net.BroadcastUnreliable(pkt);
            }
        }
    }
}
