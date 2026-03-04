using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(PlayerStats), "AddXp")]
    public static class XpEventPatch
    {
        static void Postfix(float amount)
        {
            if (XpSyncHandler.IsApplyingRemoteXp) return;
            if (!CoopState.IsHost || !CoopState.IsConnected) return;

            float share = XpSyncHandler.GetBroadcastAmount(amount);
            if (share > 0f) EventBridge.OnXpGained(share);
        }
    }
}
