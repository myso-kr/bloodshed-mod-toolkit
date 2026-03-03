using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(PlayerStats), "AddXp")]
    public static class XpEventPatch
    {
        // NetManager.HandleXpGained / XpSyncHandler.ApplyLevelUp 에서 설정 — 재귀 방지
        internal static bool _applyingRemoteXp = false;

        static void Postfix(float amount)
        {
            if (_applyingRemoteXp) return;
            if (!CoopState.IsHost || !CoopState.IsConnected) return;

            float share = XpSyncHandler.GetBroadcastAmount(amount);
            if (share > 0f) EventBridge.OnXpGained(share);
        }
    }
}
