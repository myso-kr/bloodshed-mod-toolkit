using System.Collections.Generic;
using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 속사 (Rapid Fire) —
    /// ShotAction.SetCooldownEnd Postfix 에서 CooldownEnd 와 shotDelay 를 0 으로 강제합니다.
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
            {
                __instance.CooldownEnd = 0f;
                __instance.shotDelay   = 0f;
            }
        }
    }

    /// <summary>
    /// 무기 Animator 배속 및 shotDelay 동기화 —
    /// 속사/발사속도 배율이 활성 중일 때 Weapon.animator.speed 와 shotDelay 를 게임플레이
    /// 배율에 맞춰 조정합니다. 애니메이션이 게임플레이보다 느리면 상태머신이 다음 발사를
    /// 블락하고, shotDelay WaitForSeconds 가 남아 있으면 코루틴이 Idle 타임을 소모하므로
    /// 두 값을 모두 동기화해야 속사가 올바르게 동작합니다.
    ///
    /// 우선순위: RapidFire(100×) &gt; WeaponFireRateMult(&gt;1) &gt; 원본 복원(1×)
    /// </summary>
    [HarmonyPatch(typeof(Weapon), "Update")]
    public static class WeaponAnimSpeedPatch
    {
        // 인스턴스별 원본 shotDelay 캐시 — 배율 복원 시 사용
        private static readonly Dictionary<int, float> _origShotDelay = new();

        static void Postfix(Weapon __instance)
        {
            int id = __instance.GetInstanceID();

            float mult;
            if (CheatState.RapidFire)
            {
                mult = 100f;
            }
            else
            {
                mult = TweakState.Current.WeaponFireRateMult;
                if (mult <= 1f)
                {
                    // 배율 없음 — shotDelay 원본 복원
                    if (_origShotDelay.TryGetValue(id, out float orig))
                        __instance.shotDelay = orig;
                    return;
                }
            }

            // 원본 shotDelay 첫 기록 (아직 0이 아닐 때만)
            if (!_origShotDelay.ContainsKey(id) && __instance.shotDelay > 0f)
                _origShotDelay[id] = __instance.shotDelay;

            // Animator 배속
            if (__instance.animator != null)
            {
                __instance.animator.speed = mult;
                if (!string.IsNullOrEmpty(__instance.strReloadSpeed))
                    __instance.animator.SetFloat(__instance.strReloadSpeed, mult);
            }

            // shotDelay 조정: RapidFire → 0, WeaponFireRateMult → 원본/배율
            __instance.shotDelay = CheatState.RapidFire
                ? 0f
                : (_origShotDelay.TryGetValue(id, out float o) ? o / mult : __instance.shotDelay / mult);
        }
    }
}
