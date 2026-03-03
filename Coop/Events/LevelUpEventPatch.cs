using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(PlayerStats), "LevelUpChecker")]
    public static class LevelUpEventPatch
    {
        static void Postfix(PlayerStats __instance)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            EventBridge.OnLevelUp(__instance.level);
        }
    }
}
