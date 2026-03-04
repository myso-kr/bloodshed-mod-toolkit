using System.Collections.Generic;
using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 속사 (Rapid Fire) —
    /// ShotAction.SetCooldownEnd Postfix 에서 CooldownEnd 와 shotDelay 를 0 으로 강제합니다.
    /// RapidFire 해제 시에는 원본 shotDelay 를 복원합니다.
    /// </summary>
    [HarmonyPatch(typeof(ShotAction), nameof(ShotAction.SetCooldownEnd))]
    public static class RapidFirePatch
    {
        private static readonly Dictionary<int, float> _origShotDelay = new();

        static void Postfix(ShotAction __instance)
        {
            int id = __instance.GetInstanceID();

            if (CheatState.RapidFire)
            {
                __instance.CooldownEnd = 0f;

                // 원본 shotDelay 첫 기록
                if (!_origShotDelay.ContainsKey(id) && __instance.shotDelay > 0f)
                    _origShotDelay[id] = __instance.shotDelay;

                __instance.shotDelay = 0f;
            }
            else
            {
                // RapidFire 해제 → 원본 복원
                if (_origShotDelay.TryGetValue(id, out float orig))
                    __instance.shotDelay = orig;
            }
        }
    }

    /// <summary>
    /// 무기 Animator 배속 및 shotDelay 동기화 —
    /// 속사/발사속도 배율이 활성 중일 때 Weapon.animator.speed 와 shotDelay 를 게임플레이
    /// 배율에 맞춰 조정합니다.
    ///
    /// RapidFire/WeaponFireRateMult 해제 시 animator.speed = 1, shotDelay = 원본으로 복원.
    /// 우선순위: RapidFire(100×) &gt; WeaponFireRateMult(&gt;1) &gt; 원본 복원(1×)
    /// </summary>
    [HarmonyPatch(typeof(Weapon), "Update")]
    public static class WeaponAnimSpeedPatch
    {
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
                    // 배율 없음 — animator.speed + shotDelay 원본 복원
                    if (__instance.animator != null)
                        __instance.animator.speed = 1f;
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
