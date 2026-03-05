using System.Collections.Generic;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Debug
{
    public record PeerDebugState(
        string SceneName,
        string CharacterName,
        string MissionScene,
        float  LastSeenTime);   // Time.time

    public static class PeerInfoStore
    {
        private static readonly Dictionary<ulong, PeerDebugState>        _peers    = new();
        private static readonly Dictionary<ulong, Dictionary<byte, int>> _rxCounts = new();
        private static readonly Dictionary<byte, int>                    _empty    = new();

        // 자신의 현재 상태 (CoopDebugPanel이 1초마다 갱신)
        public static string SelfScene         { get; set; } = "";
        public static string SelfCharacterName { get; set; } = "";
        public static string SelfMissionScene  { get; set; } = "";

        public static IReadOnlyDictionary<ulong, PeerDebugState> Peers => _peers;

        public static void OnPeerInfo(ulong steamId, string scene, string charName, string mission)
            => _peers[steamId] = new PeerDebugState(scene, charName, mission, Time.time);

        // PacketRouter.Dispatch()에서 호출 — 모든 수신 패킷 집계
        public static void OnPacketReceived(ulong from, byte type)
        {
            if (!_rxCounts.TryGetValue(from, out var dict))
                _rxCounts[from] = dict = new();
            dict.TryGetValue(type, out int c);
            dict[type] = c + 1;
        }

        public static bool TryGetPeer(ulong id, out PeerDebugState s) => _peers.TryGetValue(id, out s!);
        public static IReadOnlyDictionary<byte, int> GetRxCounts(ulong id)
            => _rxCounts.TryGetValue(id, out var d) ? d : _empty;

        public static void Remove(ulong id) { _peers.Remove(id); _rxCounts.Remove(id); }
        public static void Reset()
        {
            _peers.Clear();
            _rxCounts.Clear();
            SelfScene = SelfCharacterName = SelfMissionScene = "";
        }
    }
}
