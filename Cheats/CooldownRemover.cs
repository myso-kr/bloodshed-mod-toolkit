using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// PlayerStats.RecalculateStats Postfix —
    /// Cooldown 스탯을 극대화해 액티브 어빌리티 충전 속도를 최대로 만듭니다.
    ///
    /// 주의: ActiveAbilityHandler.ProcessActiveAbilities 직접 후킹은
    /// IL2CPP 트램폴린 초기화 문제로 NullReferenceException 를 유발하므로 사용하지 않습니다.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "RecalculateStats")]
    public static class CooldownStatPatch
    {
        static void Postfix(PlayerStats __instance)
        {
            if (CheatState.NoCooldown)
                __instance.Cooldown = 9999f;
        }
    }
}
