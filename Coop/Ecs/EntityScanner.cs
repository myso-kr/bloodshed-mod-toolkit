using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
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
        private float   _scanTimer;
        private float   _fullSnapshotTimer;
        private World?  _world;
        private bool    _ecsReady;
        private EntityManager _em;
        private EntityQuery   _enemyQuery;
        private uint    _tick;

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

        void Start()
        {
            TryInitEcs();
        }

        void OnDestroy()
        {
            // 게임 종료 시 ECS World가 먼저 해제될 수 있으므로 유효성 확인 후 Dispose
            if (_ecsReady && _world != null && _world.IsCreated)
            {
                try { _enemyQuery.Dispose(); }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[EntityScanner] Dispose 오류 (무시): {ex.Message}");
                }
            }
            _ecsReady = false;
            Instance = null;
        }

        /// <summary>현재 스냅샷 읽기 전용 뷰 (FullSnapshot 전송용).</summary>
        public IReadOnlyDictionary<uint, EnemySnapshot> GetCurrentSnapshot()
            => _lastSnapshot;

        void LateUpdate()
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;

            // ECS World가 씬 전환 후 재생성됐을 수 있으므로 재확인
            if (!_ecsReady)
                TryInitEcs();

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
        // 초기화
        // ════════════════════════════════════════════════════════════════════
        private void TryInitEcs()
        {
            try
            {
                _world = World.DefaultGameObjectInjectionWorld;
                if (_world == null || !_world.IsCreated)
                    return;

                _em = _world.EntityManager;
                _enemyQuery = _em.CreateEntityQuery(
                    ComponentType.ReadOnly<LocalTransform>());
                _ecsReady = true;
                Plugin.Log.LogInfo("[EntityScanner] ECS 초기화 완료");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[EntityScanner] ECS 초기화 실패: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 스캔
        // ════════════════════════════════════════════════════════════════════
        private Dictionary<uint, EnemySnapshot> ScanEnemies()
        {
            var result = new Dictionary<uint, EnemySnapshot>();

            // 방법 A: ECS EntityQuery (LocalTransform 기반)
            if (_ecsReady && _world != null && _world.IsCreated)
            {
                try
                {
                    _em.CompleteAllTrackedJobs();
                    var entities = _enemyQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var e = entities[i];
                        if (!_em.HasComponent<LocalTransform>(e)) continue;

                        var lt = _em.GetComponentData<LocalTransform>(e);
                        result[(uint)e.Index] = new EnemySnapshot
                        {
                            HostEntityIndex = (uint)e.Index,
                            PosX = lt.Position.x,
                            PosY = lt.Position.y,
                            PosZ = lt.Position.z,
                            Hp   = 65535,   // Phase 4에서 ECS Health 컴포넌트로 교체
                        };
                    }
                    entities.Dispose();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[EntityScanner] ECS 스캔 오류: {ex.Message}");
                    // _ecsReady는 false로 설정하지 않음 — 다음 프레임에 자동 재시도
                }
            }

            // 방법 B: MonoBehaviour 폴백
            var cards = UnityEngine.Object.FindObjectsOfType<EnemyIdentityCard>();
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    uint id = (uint)card.GetInstanceID();
                    if (result.ContainsKey(id)) continue;   // ECS 결과 우선
                    result[id] = new EnemySnapshot
                    {
                        HostEntityIndex = id,
                        PosX = 0f, PosY = 0f, PosZ = 0f,  // Phase 4에서 정밀화
                        Hp   = 0,
                    };
                }
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        // 델타 압축
        // ════════════════════════════════════════════════════════════════════
        private List<EnemySnapshot> ComputeDelta(Dictionary<uint, EnemySnapshot> current)
        {
            var changed = new List<EnemySnapshot>();
            const float  PosThresholdSq = 0.1f * 0.1f;
            const ushort HpThreshold    = 10;

            foreach (var (id, snap) in current)
            {
                if (!_lastSnapshot.TryGetValue(id, out var prev))
                {
                    changed.Add(snap);  // 신규 엔티티
                    continue;
                }

                float dx = snap.PosX - prev.PosX;
                float dy = snap.PosY - prev.PosY;
                float dz = snap.PosZ - prev.PosZ;

                if (dx*dx + dy*dy + dz*dz > PosThresholdSq ||
                    Math.Abs(snap.Hp - prev.Hp) > HpThreshold)
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
