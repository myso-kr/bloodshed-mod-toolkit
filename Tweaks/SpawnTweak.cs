using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Tweaks
{
    /// <summary>
    /// 최대 에너미 수 배율 — SpawnProcessor.GetMaxEnemyCount() Postfix.
    /// 반환된 최대 동시 에너미 수에 SpawnCountMult를 적용합니다.
    /// </summary>
    [HarmonyPatch(typeof(SpawnProcessor), "GetMaxEnemyCount")]
    public static class SpawnMaxCountTweakPatch
    {
        static void Postfix(ref int __result)
        {
            var mult = TweakState.Current.SpawnCountMult;
            if (mult == 1f) return;
            __result = Math.Max(1, (int)(__result * mult));
        }
    }

    /// <summary>
    /// 스폰 수량 배율 — SpawnDirector.SpawnEnemies Prefix.
    /// 한 번의 스폰 호출에서 생성되는 에너미 수에 SpawnCountMult를 적용합니다.
    /// </summary>
    [HarmonyPatch(typeof(SpawnDirector), "SpawnEnemies",
        new Type[] { typeof(Transform), typeof(int), typeof(bool) })]
    public static class SpawnAmountTweakPatch
    {
        static void Prefix(ref int spawnAmount)
        {
            var mult = TweakState.Current.SpawnCountMult;
            if (mult == 1f) return;
            spawnAmount = Math.Max(1, (int)(spawnAmount * mult));
        }
    }
}
