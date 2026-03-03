using HarmonyLib;
using com8com1.SCFPS;
using UnityEngine;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// Q3PlayerController.Accelerate Prefix —
    /// targetSpeed / accel 양쪽을 배율만큼 증폭합니다.
    ///
    /// 이전의 GetPlayerSpeed Postfix 방식은 반환값만 바꾸고
    /// 실제 이동 계산(GroundMove/AirMove)에 영향이 없었습니다.
    /// Accelerate 는 실제 물리 이동 계산의 핵심 함수입니다.
    /// </summary>
    [HarmonyPatch(typeof(Q3PlayerController), "Accelerate")]
    public static class SpeedHackPatch
    {
        static void Prefix(ref float targetSpeed, ref float accel)
        {
            if (CheatState.SpeedHack)
            {
                targetSpeed *= CheatState.SpeedMultiplier;
                accel       *= CheatState.SpeedMultiplier;
            }
        }
    }

    /// <summary>
    /// 공중 이동(AirControl)도 같은 배율 적용.
    /// </summary>
    [HarmonyPatch(typeof(Q3PlayerController), "AirControl")]
    public static class SpeedHackAirPatch
    {
        static void Prefix(ref float targetSpeed)
        {
            if (CheatState.SpeedHack)
                targetSpeed *= CheatState.SpeedMultiplier;
        }
    }
}
