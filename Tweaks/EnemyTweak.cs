using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;

namespace BloodshedModToolkit.Tweaks
{
    // ── 에너미 속도 패치 내부 공유 상태 ───────────────────────────────────────────
    // SetBehaviorWalkable 과 RefreshAgentSpeed 두 패치 클래스가 공유합니다.

    internal static class EnemySpeedPatchState
    {
        /// <summary>RefreshAgentSpeed 실행 중 여부. ThreadStatic 으로 재진입 안전.</summary>
        [ThreadStatic]
        internal static bool InRefresh;

        /// <summary>현재 RefreshAgentSpeed 실행 중 SetBehaviorWalkable 가 호출됐는지.</summary>
        [ThreadStatic]
        internal static bool WalkableFiredInRefresh;

        /// <summary>
        /// 에너미 인스턴스 포인터 → 배율 미적용 기본 속도 캐시.
        /// SetBehaviorWalkable Prefix 에서 배율 적용 전 원본 값을 저장합니다.
        /// RefreshAgentSpeed 가 SetBehaviorWalkable 을 우회할 때 강제 호출에 사용됩니다.
        /// </summary>
        internal static readonly Dictionary<IntPtr, float> BaseSpeedCache = new();
    }

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
    /// 에너미 이동속도 — SetBehaviorWalkable Prefix.
    ///
    /// 기본 속도(배율 미적용)를 인스턴스별로 캐시하고 EnemySpeedMult 를 적용합니다.
    /// RefreshAgentSpeed 실행 중 호출 여부를 EnemySpeedPatchState 를 통해 추적하여
    /// EnemySpeedRefreshGuardPatch 가 경로 분기를 판별할 수 있도록 합니다.
    /// </summary>
    [HarmonyPatch(typeof(EnemyAbilityController), "SetBehaviorWalkable")]
    public static class EnemySpeedTweakPatch
    {
        static void Prefix(EnemyAbilityController __instance, ref float totalSpeed)
        {
            // RefreshAgentSpeed 실행 중이라면 호출 발생을 기록
            if (EnemySpeedPatchState.InRefresh)
                EnemySpeedPatchState.WalkableFiredInRefresh = true;

            // 배율 적용 전 원본 속도를 인스턴스별로 저장
            EnemySpeedPatchState.BaseSpeedCache[__instance.Pointer] = totalSpeed;

            var mult = TweakState.Current.EnemySpeedMult;
            if (mult != 1f) totalSpeed *= mult;
        }
    }

    /// <summary>
    /// RefreshAgentSpeed 보호 패치 — 속도 배율 확실한 적용 보장.
    ///
    /// 게임 구현에 따라 두 가지 경로가 존재합니다.
    ///
    /// [경로 A] RefreshAgentSpeed → SetBehaviorWalkable(baseSpeed) 내부 호출
    ///   WalkableFiredInRefresh == true → Postfix 에서 별도 처리 없음.
    ///   EnemySpeedTweakPatch.Prefix 에서 이미 배율이 적용됨.
    ///
    /// [경로 B] RefreshAgentSpeed → navAgent.speed 직접 설정 (SetBehaviorWalkable 우회)
    ///   WalkableFiredInRefresh == false → Postfix 에서 캐시된 기본 속도로
    ///   SetBehaviorWalkable 을 강제 호출 → EnemySpeedTweakPatch.Prefix 가 배율 적용.
    /// </summary>
    [HarmonyPatch(typeof(EnemyAbilityController), "RefreshAgentSpeed")]
    public static class EnemySpeedRefreshGuardPatch
    {
        static void Prefix()
        {
            EnemySpeedPatchState.InRefresh            = true;
            EnemySpeedPatchState.WalkableFiredInRefresh = false;
        }

        static void Postfix(EnemyAbilityController __instance)
        {
            EnemySpeedPatchState.InRefresh = false;

            // 경로 A: SetBehaviorWalkable 이 이미 호출되어 배율 적용 완료
            if (EnemySpeedPatchState.WalkableFiredInRefresh) return;

            // 경로 B: SetBehaviorWalkable 이 호출되지 않음 — 캐시 기반으로 강제 적용
            var mult = TweakState.Current.EnemySpeedMult;
            if (mult == 1f) return;

            if (EnemySpeedPatchState.BaseSpeedCache.TryGetValue(__instance.Pointer, out float baseSpeed)
                && baseSpeed > 0f)
            {
                // 캐시된 원본 속도로 SetBehaviorWalkable 호출
                // → EnemySpeedTweakPatch.Prefix 가 배율을 적용하여 navAgent 에 설정
                __instance.SetBehaviorWalkable(baseSpeed);
            }
        }
    }
}
