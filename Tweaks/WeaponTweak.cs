using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Tweaks
{
    /// <summary>무기 데미지 배율 — WeaponItem.GetDamageTotal() Postfix.</summary>
    [HarmonyPatch(typeof(WeaponItem), "GetDamageTotal")]
    public static class WeaponDamageTweakPatch
    {
        static void Postfix(ref float __result)
        {
            __result *= TweakState.Current.WeaponDamageMult;
        }
    }

    /// <summary>
    /// 발사속도 배율 — WeaponItem.GetCooldownTotal() Postfix.
    /// 반환값(쿨다운 시간)을 배율로 나누면 발사속도가 그만큼 빨라집니다.
    /// </summary>
    [HarmonyPatch(typeof(WeaponItem), "GetCooldownTotal")]
    public static class WeaponFireRateTweakPatch
    {
        static void Postfix(ref float __result)
        {
            var mult = TweakState.Current.WeaponFireRateMult;
            if (mult > 0f) __result /= mult;
        }
    }

    /// <summary>
    /// 장전속도 배율 — WeaponItem.GetReloadDurationTotal() Postfix.
    /// 반환값(장전 시간)을 배율로 나누면 장전이 그만큼 빨라집니다.
    /// </summary>
    [HarmonyPatch(typeof(WeaponItem), "GetReloadDurationTotal")]
    public static class WeaponReloadTweakPatch
    {
        static void Postfix(ref float __result)
        {
            var mult = TweakState.Current.WeaponReloadSpeedMult;
            if (mult > 0f) __result /= mult;
        }
    }
}
