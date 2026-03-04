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
            float hp, float maxHp, int level, float xp, float xpCap,
            float rotY = 0f, byte weaponClassId = 0, byte charId = 0)
        {
            if (!CoopState.IsConnected || Net == null) return;
            Net.BroadcastUnreliable(
                PlayerStatePacket.Encode(steamId, px, py, pz, hp, maxHp, level, xp, xpCap,
                    rotY, weaponClassId, charId));
        }

        /// <summary>공격 이벤트 브로드캐스트 — 피어 아바타의 공격 애니메이션 동기화.</summary>
        public static void OnAttack(ulong steamId)
        {
            if (!CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(AttackEventPacket.Encode(steamId));
        }

        /// <summary>
        /// Guest → Host: 적 데미지 요청.
        /// Guest가 자체적으로 Hit 판정 후 Host에게 권위 있는 데미지 적용을 요청.
        /// </summary>
        public static void OnDamageRequest(uint hostEntityIdx, float damage)
        {
            // Guest만 호출 (IsHost가 아닐 때)
            if (CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(DamageRequestPacket.Encode(hostEntityIdx, damage));
        }

        /// <summary>채팅 메시지 — Host/Guest 양방향, 연결된 피어에게 브로드캐스트.</summary>
        public static void OnChatMessage(string senderName, string message)
        {
            if (!CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(ChatMessagePacket.Encode(senderName, message));
        }

        public static void OnMissionStart(string sceneName, int buildIndex)
        {
            if (!CoopState.IsHost) return;
            if (!CoopState.IsConnected || Net == null)
            {
                Plugin.Log.LogInfo($"[EventBridge] MissionStart: '{sceneName}' — 연결된 게스트 없음 (브로드캐스트 생략)");
                return;
            }
            Net.BroadcastReliable(MissionStartPacket.Encode(sceneName, buildIndex));
            Plugin.Log.LogInfo($"[EventBridge] MissionStart 브로드캐스트: '{sceneName}' (idx={buildIndex}) → {CoopState.Peers.Count}명");
        }

        public static void OnMissionBriefing(string sceneName, int buildIndex)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected || Net == null) return;
            Net.BroadcastReliable(MissionBriefingPacket.Encode(sceneName, buildIndex));
            Plugin.Log.LogInfo($"[EventBridge] MissionBriefing 브로드캐스트: '{sceneName}' (idx={buildIndex}) → {CoopState.Peers.Count}명");
        }
    }
}
