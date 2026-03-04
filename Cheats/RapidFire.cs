using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>속사 배율 — 모든 패치에서 공유하는 단일 상수.</summary>
    internal static class RapidFireConst
    {
        public const float Mult = 5f;
    }

    /// <summary>
    /// 속사 (Rapid Fire) —
    /// ShotAction.SetCooldownEnd Postfix 에서 CooldownEnd 를 0 으로 강제하고
    /// shotDelay 를 Mult 배율로 단축합니다.
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

                if (!_origShotDelay.ContainsKey(id) && __instance.shotDelay > 0f)
                    _origShotDelay[id] = __instance.shotDelay;

                __instance.shotDelay = _origShotDelay.TryGetValue(id, out float orig)
                    ? orig / RapidFireConst.Mult
                    : 0f;
            }
            else
            {
                if (_origShotDelay.TryGetValue(id, out float orig))
                    __instance.shotDelay = orig;
            }
        }
    }

    /// <summary>
    /// 무기 Animator 배속 및 shotDelay 동기화 —
    /// RapidFire(Mult×) / WeaponFireRateMult(&gt;1) 활성 시 속도 조정.
    /// 해제 시 animator.speed = 1, shotDelay = 원본으로 복원.
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
                mult = RapidFireConst.Mult;
            else
            {
                mult = TweakState.Current.WeaponFireRateMult;
                if (mult <= 1f)
                {
                    if (__instance.animator != null)
                        __instance.animator.speed = 1f;
                    if (_origShotDelay.TryGetValue(id, out float orig))
                        __instance.shotDelay = orig;
                    return;
                }
            }

            if (!_origShotDelay.ContainsKey(id) && __instance.shotDelay > 0f)
                _origShotDelay[id] = __instance.shotDelay;

            if (__instance.animator != null)
            {
                __instance.animator.speed = mult;
                if (!string.IsNullOrEmpty(__instance.strReloadSpeed))
                    __instance.animator.SetFloat(__instance.strReloadSpeed, mult);
            }

            __instance.shotDelay = _origShotDelay.TryGetValue(id, out float o)
                ? o / mult
                : __instance.shotDelay / mult;
        }
    }

    /// <summary>
    /// WeaponHandler 발동 쿨다운 + 버스트 타이머 단축 —
    /// activationCooldown: RapidFire 시 Mult배 빠르게 감소 (즉시 0 설정 대신 자연스럽게).
    /// burstShotTimer: 버스트 간격 조건이 즉시 충족되도록 MaxValue 설정.
    /// </summary>
    [HarmonyPatch(typeof(WeaponHandler), "Update")]
    public static class WeaponHandlerRapidFirePatch
    {
        static void Postfix(WeaponHandler __instance)
        {
            float mult;
            if (CheatState.RapidFire)
                mult = RapidFireConst.Mult;
            else
            {
                mult = TweakState.Current.WeaponFireRateMult;
                if (mult <= 1f) return;
            }

            // activationCooldown 을 배율만큼 추가 감소 (자연 감소 + 추가 감소)
            if (__instance.activationCooldown > 0f)
                __instance.activationCooldown = Mathf.Max(
                    0f,
                    __instance.activationCooldown - Time.deltaTime * (mult - 1f));

            // 버스트 무기: 간격 조건을 즉시 충족
            if (CheatState.RapidFire)
                __instance.burstShotTimer = float.MaxValue;
        }
    }
}
