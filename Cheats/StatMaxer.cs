using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// PlayerStats.RecalculateStats Postfix —
    /// MaxStats ON 시 스탯 재계산 직후 모든 수치를 최대로 덮어씁니다.
    /// OneShotKill 은 Health.Damage 후킹으로 이전하여 여기서 제거.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "RecalculateStats")]
    public static class StatMaxerPatch
    {
        static void Postfix(PlayerStats __instance)
        {
            if (!CheatState.MaxStats) return;

            try
            {
                __instance.MaxHp      = 99999f;
                __instance.Armor      = 9999f;
                __instance.Agility    = 9999f;
                __instance.Might      = 9999f;
                __instance.Area       = 9999f;
                __instance.Duration   = 9999f;
                __instance.Speed      = 9999f;
                __instance.Cooldown   = 9999f;
                __instance.Luck       = 9999f;
                __instance.Bloodthirst= 9999f;
                __instance.Accuracy   = 9999f;
                __instance.Attraction = 9999f;
                __instance.RestoreHp(99999f);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[StatMaxer] 스탯 설정 실패: {ex.Message}");
            }
        }
    }
}
