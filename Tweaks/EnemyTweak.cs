using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;

namespace BloodshedModToolkit.Tweaks
{
    /// <summary>
    /// 에너미 체력/데미지 통합 패치 — Health.Damage Prefix.
    ///
    /// • isPlayer == false (에너미 체력):
    ///     에너미가 받는 피해를 EnemyHpMult 역수로 스케일.
    ///     EnemyHpMult = 2.0 → 피해 ÷ 2 → 실질 체력 2배 효과.
    ///
    /// • isPlayer == true (플레이어 체력):
    ///     에너미로부터 받는 피해를 EnemyDamageMult로 스케일.
    ///     EnemyDamageMult = 1.5 → 피해 × 1.5 → 더 아픕니다.
    /// </summary>
    [HarmonyPatch(typeof(Health), "Damage",
        new Type[] { typeof(float), typeof(GameObject),
                     typeof(float), typeof(float),
                     typeof(Vector3), typeof(Vector3), typeof(bool) })]
    public static class EnemyHpAndDamageTweakPatch
    {
        static void Prefix(Health __instance, ref float damage)
        {
            var c = TweakState.Current;
            if (__instance.isPlayer)
            {
                if (c.EnemyDamageMult != 1f)
                    damage *= c.EnemyDamageMult;
            }
            else
            {
                if (c.EnemyHpMult > 0f && c.EnemyHpMult != 1f)
                    damage /= c.EnemyHpMult;
            }
        }
    }

    /// <summary>
    /// 에너미 이동속도 배율 — EnemyAbilityController.SetBehaviorWalkable Prefix.
    /// 내비게이션 에이전트에 설정되는 totalSpeed에 배율을 적용합니다.
    /// </summary>
    [HarmonyPatch(typeof(EnemyAbilityController), "SetBehaviorWalkable")]
    public static class EnemySpeedTweakPatch
    {
        static void Prefix(ref float totalSpeed)
        {
            var mult = TweakState.Current.EnemySpeedMult;
            if (mult != 1f) totalSpeed *= mult;
        }
    }
}
