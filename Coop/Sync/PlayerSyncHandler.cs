using System.Collections.Generic;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Sync
{
    /// <summary>
    /// 원격 플레이어(Peer) 상태 저장소.
    /// NetManager.HandlePlayerState 에서 갱신되고,
    /// CheatMenu Co-op 탭 PEERS 섹션에서 읽어 UI를 그립니다.
    /// </summary>
    public static class PlayerSyncHandler
    {
        private static readonly Dictionary<ulong, PlayerStatePacket> _states = new();

        /// <summary>알려진 모든 Peer 상태의 읽기 전용 뷰.</summary>
        public static IReadOnlyDictionary<ulong, PlayerStatePacket> States => _states;

        /// <summary>NetManager에서 PlayerState 패킷 수신 시 호출.</summary>
        public static void OnPlayerState(ulong steamId, PlayerStatePacket pkt)
            => _states[steamId] = pkt;

        /// <summary>특정 Peer 상태 조회.</summary>
        public static bool TryGetState(ulong steamId, out PlayerStatePacket pkt)
            => _states.TryGetValue(steamId, out pkt);

        /// <summary>세션 종료 시 초기화.</summary>
        public static void Reset() => _states.Clear();
    }
}
