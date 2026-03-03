using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Events;

namespace BloodshedModToolkit.Coop.Sync
{
    /// <summary>
    /// Co-op XP / 레벨 동기화 로직.
    /// XpEventPatch 및 NetManager.HandleLevelUp 에서 호출됩니다.
    /// </summary>
    public static class XpSyncHandler
    {
        /// <summary>
        /// Host에서 XP 이벤트 발생 시 Guest에게 보낼 XP 양을 반환합니다.
        /// Independent 모드에서는 0을 반환 (전송 안 함).
        /// </summary>
        public static float GetBroadcastAmount(float earned) =>
            CoopConfig.XpShare switch
            {
                XpShareMode.Independent => 0f,
                XpShareMode.Split       => earned * 0.5f,
                _                       => earned,   // Replicate
            };

        /// <summary>
        /// Guest에서 LevelUp 패킷 수신 시 레벨 동기화를 적용합니다.
        /// </summary>
        public static void ApplyLevelUp(int newLevel)
        {
            var ps = Object.FindObjectOfType<PlayerStats>();
            if (ps == null)
            {
                Plugin.Log.LogWarning("[XpSyncHandler] PlayerStats 없음 — LevelUp 스킵");
                return;
            }
            if (ps.level >= newLevel) return;   // 이미 해당 레벨 이상

            // SetLevel 중 AddXp 후킹이 재귀 브로드캐스트 하지 않도록 플래그 설정
            XpEventPatch._applyingRemoteXp = true;
            try   { ps.SetLevel(newLevel); }
            finally { XpEventPatch._applyingRemoteXp = false; }

            ps.RecalculateStats();
            Plugin.Log.LogInfo($"[XpSyncHandler] 레벨 동기화: {ps.level} → {newLevel}");
        }
    }
}
