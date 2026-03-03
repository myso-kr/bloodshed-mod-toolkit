using BloodshedModToolkit.Coop.Net;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// Harmony 패치에서 발생한 게임 이벤트를 NetManager로 전달하는 정적 중개자.
    /// 패치 클래스에서 직접 호출 가능.
    /// </summary>
    public static class EventBridge
    {
        private static NetManager? Net => NetManager.Instance;

        public static void OnEnemySpawned(uint hostEntityIdx, ushort typeId,
                                          float x, float y, float z, uint seed)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(EntitySpawnPacket.Encode(hostEntityIdx, typeId, x, y, z, seed));
        }

        public static void OnEnemyDespawned(uint hostEntityIdx)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(EntityDespawnPacket.Encode(hostEntityIdx));
        }

        public static void OnWaveAdvanced(int waveIdx, uint seed)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(WaveAdvancePacket.Encode(waveIdx, seed));
        }

        public static void OnXpGained(float amount, uint srcIdx = 0)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(XpGainedPacket.Encode(amount, srcIdx));
        }

        public static void OnLevelUp(int newLevel)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(LevelUpPacket.Encode(newLevel));
        }

        public static void OnItemSelected(int itemIndex)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(ItemSelectedPacket.Encode(itemIndex));
        }

        public static void OnPlayerStateChanged(ulong steamId,
            float px, float py, float pz,
            float hp, float maxHp, int level, float xp, float xpCap)
        {
            if (!CoopState.IsConnected || Net == null) return;
            Net.BroadcastUnreliable(
                PlayerStatePacket.Encode(steamId, px, py, pz, hp, maxHp, level, xp, xpCap));
        }
    }
}
