using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Tweaks
{
    /// <summary>
    /// 플레이어 체력 배율 — RecalculateStats 후 MaxHp에 적용.
    /// RecalculateStats 는 baseStats 기반으로 매번 재계산하므로 Postfix 곱셈이 누적되지 않습니다.
    /// Priority.High 로 StatMaxerPatch(Normal) 보다 먼저 실행되어 HP 배율을 먼저 적용합니다.
    /// MaxStats ON 시 StatMaxerPatch 가 이후 99999 로 덮어씁니다 (정상 동작).
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "RecalculateStats")]
    [HarmonyPriority(Priority.High)]
    public static class PlayerHpTweakPatch
    {
        static void Postfix(PlayerStats __instance)
        {
            var mult = TweakState.Current.PlayerHpMult;
            if (mult == 1f) return;

            float prevMax = __instance.MaxHp;
            __instance.MaxHp = prevMax * mult;

            // CurrentHp 비율 유지
            if (prevMax > 0f)
                __instance.CurrentHp = __instance.CurrentHp * (__instance.MaxHp / prevMax);
        }
    }

    /// <summary>
    /// 플레이어 이동속도 배율 — Q3PlayerController.Accelerate Prefix.
    /// SpeedHack 패치와 독립적으로 작동하며 두 배율이 곱해집니다.
    /// </summary>
    [HarmonyPatch(typeof(Q3PlayerController), "Accelerate")]
    public static class PlayerSpeedTweakPatch_Accelerate
    {
        static void Prefix(ref float targetSpeed, ref float accel)
        {
            var mult = TweakState.Current.PlayerSpeedMult;
            if (mult == 1f) return;
            targetSpeed *= mult;
            accel       *= mult;
        }
    }

    /// <summary>공중 이동(AirControl)도 동일한 속도 배율 적용.</summary>
    [HarmonyPatch(typeof(Q3PlayerController), "AirControl")]
    public static class PlayerSpeedTweakPatch_AirControl
    {
        static void Prefix(ref float targetSpeed)
        {
            var mult = TweakState.Current.PlayerSpeedMult;
            if (mult == 1f) return;
            targetSpeed *= mult;
        }
    }
}
