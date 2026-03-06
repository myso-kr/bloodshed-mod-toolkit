using System.Collections.Generic;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Coop.Ecs
{
    /// <summary>
    /// Co-op 세션 전역 엔티티 레지스트리.
    /// Guest 측에서 HostEntityIndex ↔ 로컬 ID 매핑을 관리.
    /// </summary>
    public static class EntityRegistry
    {
        /// <summary>HostEntityIndex → 로컬 ID 매핑 테이블.</summary>
        public static EntityIdMapper HostToLocal { get; } = new();

        /// <summary>
        /// HandleEntitySpawn 수신 시 큐잉되는 HostEntityIndex 대기열.
        /// Guest의 SpawnEventPatch가 실행될 때 순서대로 매핑.
        /// </summary>
        public static Queue<uint> PendingHostIds { get; } = new();

        /// <summary>
        /// localId(GetInstanceID) → Health 컴포넌트 캐시.
        /// Host/Guest 양측에서 SpawnEventPatch가 등록, StateApplicator/DamageRequest가 참조.
        /// </summary>
        public static Dictionary<int, Health> LocalHealth { get; } = new();

        /// <summary>씬 전환 또는 세션 종료 시 전체 초기화.</summary>
        public static void Reset()
        {
            HostToLocal.Clear();
            PendingHostIds.Clear();
            LocalHealth.Clear();
        }
    }
}
