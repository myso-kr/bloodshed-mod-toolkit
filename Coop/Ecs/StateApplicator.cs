using System;
using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;

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
        private struct SnapEntry { public float Px, Py, Pz; public ushort Hp; }
        private readonly Dictionary<uint, SnapEntry> _pending = new();

        void Awake() => Instance = this;
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

            foreach (var (hostIdx, snap) in _pending)
                ApplyEntry(hostIdx, snap);

            _pending.Clear();
        }

        // ════════════════════════════════════════════════════════════════════
        // 단일 엔티티 HP 적용
        // ════════════════════════════════════════════════════════════════════
        private void ApplyEntry(uint hostIdx, SnapEntry snap)
        {
            if (snap.Hp == 65535) return;   // 미정 값은 건너뜀
            if (!EntityRegistry.HostToLocal.TryGetLocal(hostIdx, out int localId)) return;

            if (EntityRegistry.LocalHealth.TryGetValue(localId, out var health) && health != null)
                health.currentHealth = snap.Hp;
        }
    }
}
