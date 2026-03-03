using System.Collections.Generic;

namespace BloodshedModToolkit.Coop.Ecs
{
    /// <summary>
    /// Host EntityIndex ↔ Guest 로컬 ID (ECS Entity.Index 또는 MB instanceID) 양방향 매핑.
    /// 순수 C# — Unity.Entities 의존성 없음.
    /// </summary>
    public class EntityIdMapper
    {
        private readonly Dictionary<uint, int> _hostToLocal = new();
        private readonly Dictionary<int, uint> _localToHost = new();

        public int Count => _hostToLocal.Count;

        /// <summary>Host EntityIndex와 로컬 ID(ECS Index 또는 MB instanceID)를 등록.</summary>
        public void Register(uint hostIdx, int localId)
        {
            _hostToLocal[hostIdx] = localId;
            _localToHost[localId] = hostIdx;
        }

        public bool TryGetLocal(uint hostIdx, out int localId)
            => _hostToLocal.TryGetValue(hostIdx, out localId);

        public bool TryGetHost(int localId, out uint hostIdx)
            => _localToHost.TryGetValue(localId, out hostIdx);

        public void Remove(uint hostIdx)
        {
            if (_hostToLocal.TryGetValue(hostIdx, out int localId))
            {
                _localToHost.Remove(localId);
                _hostToLocal.Remove(hostIdx);
            }
        }

        public void Clear()
        {
            _hostToLocal.Clear();
            _localToHost.Clear();
        }
    }
}
