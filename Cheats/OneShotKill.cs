using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// Health.Damage Prefix —
    /// 플레이어가 아닌 대상(= 적)에게 들어오는 데미지를 9999배 증폭합니다.
    ///
    /// Health 는 플레이어와 적 모두가 사용하는 범용 체력 컴포넌트입니다.
    /// __instance.isPlayer 로 플레이어 자신에게는 증폭하지 않습니다.
    ///
    /// 원래 Might 스탯 방식(StatMaxer Postfix)은
    ///   - Might 가 실제 데미지 공식에 선형 반영되지 않을 수 있음
    ///   - 재계산 타이밍 이슈
    /// 위 문제를 피하기 위해 데미지 적용 직전에 직접 배율을 곱합니다.
    /// </summary>
    [HarmonyPatch(typeof(Health), "Damage")]
    public static class OneShotKillPatch
    {
        static void Prefix(Health __instance, ref float damage)
        {
            if (CheatState.OneShotKill && !__instance.isPlayer)
                damage *= 9999f;
        }
    }
}
