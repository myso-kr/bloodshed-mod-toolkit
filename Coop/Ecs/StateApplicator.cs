using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace BloodshedModToolkit.Coop.Ecs
{
    /// <summary>
    /// Guest 측에서 Host의 StateSnapshot 데이터를 로컬 엔티티에 적용.
    /// LateUpdate 에서 처리하여 게임 로직과의 타이밍 충돌 방지.
    /// </summary>
    public class StateApplicator : MonoBehaviour
    {
        public StateApplicator(IntPtr ptr) : base(ptr) { }

        // ── 싱글톤 ───────────────────────────────────────────────────────────
        public static StateApplicator? Instance { get; private set; }

        // ── 보류 업데이트 버퍼 ────────────────────────────────────────────────
        // NetManager.Update → PollMessages → EnqueueUpdate 와
        // LateUpdate 모두 Unity 메인 스레드이므로 잠금 불필요.
        private struct SnapEntry { public float Px, Py, Pz; public ushort Hp; }
        private readonly Dictionary<uint, SnapEntry> _pending = new();

        // ── ECS 접근 ─────────────────────────────────────────────────────────
        private World?        _world;
        private EntityManager _em;
        private bool          _ecsReady;

        // ════════════════════════════════════════════════════════════════════
        // 생명주기
        // ════════════════════════════════════════════════════════════════════
        void Awake() => Instance = this;
        void Start()  => TryInitEcs();
        void OnDestroy() { Instance = null; }

        // ════════════════════════════════════════════════════════════════════
        // 외부 API — NetManager.HandleStateSnapshot 에서 호출
        // ════════════════════════════════════════════════════════════════════
        public void EnqueueUpdate(uint hostIdx, float px, float py, float pz, ushort hp)
            => _pending[hostIdx] = new SnapEntry { Px = px, Py = py, Pz = pz, Hp = hp };

        // ════════════════════════════════════════════════════════════════════
        // LateUpdate — 보류 업데이트 일괄 적용
        // ════════════════════════════════════════════════════════════════════
        void LateUpdate()
        {
            if (!CoopState.IsConnected || CoopState.IsHost) return;
            if (_pending.Count == 0) return;

            if (!_ecsReady) TryInitEcs();

            foreach (var (hostIdx, snap) in _pending)
                ApplyEntry(hostIdx, snap);

            _pending.Clear();
        }

        // ════════════════════════════════════════════════════════════════════
        // 단일 엔티티 적용
        // ════════════════════════════════════════════════════════════════════
        private void ApplyEntry(uint hostIdx, SnapEntry snap)
        {
            // ── ECS 위치 쓰기 ─────────────────────────────────────────────
            if (_ecsReady && EntityRegistry.EcsEntities.TryGetValue(hostIdx, out var entity))
            {
                try
                {
                    if (_em.Exists(entity))
                    {
                        var lt = _em.GetComponentData<LocalTransform>(entity);
                        lt.Position = new float3(snap.Px, snap.Py, snap.Pz);
                        _em.SetComponentData(entity, lt);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[StateApplicator] ECS 쓰기 오류: {ex.Message}");
                    _ecsReady = false;
                }
            }

            // ── MB HP 업데이트 ─────────────────────────────────────────────
            if (snap.Hp == 65535) return;   // 미정 값은 건너뜀
            if (!EntityRegistry.HostToLocal.TryGetLocal(hostIdx, out int localId)) return;

            var health = FindHealthById(localId);
            if (health != null)
                health.currentHealth = snap.Hp;
        }

        /// <summary>
        /// localId(GetInstanceID) 로 Health 컴포넌트를 찾아 LocalHealth 캐시에 등록.
        /// EnemyIdentityCard.gameObject 가 interop에서 노출되지 않으므로
        /// FindObjectsOfType 으로 탐색 후 lazy-init 캐시를 채웁니다.
        /// </summary>
        private static com8com1.SCFPS.Health? FindHealthById(int localId)
        {
            if (EntityRegistry.LocalHealth.TryGetValue(localId, out var cached) && cached != null)
                return cached;

            var all = UnityEngine.Object.FindObjectsOfType<com8com1.SCFPS.Health>();
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

        // ════════════════════════════════════════════════════════════════════
        // ECS 초기화
        // ════════════════════════════════════════════════════════════════════
        private void TryInitEcs()
        {
            try
            {
                _world = World.DefaultGameObjectInjectionWorld;
                if (_world == null || !_world.IsCreated) return;
                _em = _world.EntityManager;
                _ecsReady = true;
                Plugin.Log.LogInfo("[StateApplicator] ECS 초기화 완료");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[StateApplicator] ECS 초기화 실패: {ex.Message}");
            }
        }
    }
}
