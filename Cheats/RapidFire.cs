using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 속사 (Rapid Fire) —
    /// ShotAction.SetCooldownEnd Postfix 에서 CooldownEnd 를 0 으로 강제합니다.
    ///
    /// IsOnCooldown 게터는 "Time.time &lt; CooldownEnd" 를 반환하는 tiny getter 이므로
    /// HarmonyX 트램폴린이 적용되지 않습니다.
    /// SetCooldownEnd 는 발사 직후 반드시 호출되는 메서드이며, 여기서 종료 시각을
    /// 0 으로 덮어쓰면 IsOnCooldown 이 항상 false 가 됩니다 (Time.time > 0 이므로).
    /// </summary>
    [HarmonyPatch(typeof(ShotAction), nameof(ShotAction.SetCooldownEnd))]
    public static class RapidFirePatch
    {
        static void Postfix(ShotAction __instance)
        {
            if (CheatState.RapidFire)
                __instance.CooldownEnd = 0f;
        }
    }

    /// <summary>
    /// 무기 Animator 배속 동기화 —
    /// 속사/발사속도 배율이 활성 중일 때 Weapon.animator.speed 를 게임플레이 배율에
    /// 맞춰 조정합니다. 애니메이션이 게임플레이보다 느리면 상태머신이 다음 발사를
    /// 블락하므로, 배속을 일치시켜야 속사가 올바르게 동작합니다.
    ///
    /// 우선순위: RapidFire(100×) &gt; WeaponFireRateMult(&gt;1) &gt; 원본 유지(1×)
    /// </summary>
    [HarmonyPatch(typeof(Weapon), "Update")]
    public static class WeaponAnimSpeedPatch
    {
        static void Postfix(Weapon __instance)
        {
            if (__instance.animator == null) return;

            float mult;
            if (CheatState.RapidFire)
            {
                mult = 100f;
            }
            else
            {
                mult = TweakState.Current.WeaponFireRateMult;
                if (mult <= 1f) return; // 배율 없으면 원본 속도 유지
            }

            __instance.animator.speed = mult;

            // strReloadSpeed 파라미터가 있으면 장전 애니메이션 속도도 동기화
            if (!string.IsNullOrEmpty(__instance.strReloadSpeed))
                __instance.animator.SetFloat(__instance.strReloadSpeed, mult);
        }
    }
}
