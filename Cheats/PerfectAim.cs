using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 완벽한 조준 (Perfect Aim) —
    /// ShotAction.GetSpreadDirection 을 패치해 산탄 없이 조준 방향 그대로 발사합니다.
    /// AimPrecisionHandler.ReducePrecision 도 스킵해 정밀도 저하를 방지합니다.
    /// </summary>
    [HarmonyPatch(typeof(ShotAction), "GetSpreadDirection")]
    public static class PerfectAimSpreadPatch
    {
        static bool Prefix(Vector3 direction, ref Vector3 __result)
        {
            if (CheatState.PerfectAim)
            {
                __result = direction.normalized;
                return false;   // 원본 산탄 계산 스킵
            }
            return true;
        }
    }

    /// <summary>
    /// 조준 정밀도 감소를 차단합니다.
    /// 사격 시마다 호출되는 ReducePrecision 을 건너뛰어
    /// currentPrecision 이 항상 최대 상태로 유지됩니다.
    /// </summary>
    [HarmonyPatch(typeof(AimPrecisionHandler), "ReducePrecision")]
    public static class PerfectAimPrecisionPatch
    {
        static bool Prefix() => !CheatState.PerfectAim;
    }
}
