using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 무반동 (No Recoil) —
    /// WeaponItem.GetRecoilTotal 게터를 패치해 항상 0 을 반환합니다.
    /// 반동 계산 자체가 0 이 되므로 카메라가 위로 밀리지 않습니다.
    /// </summary>
    [HarmonyPatch(typeof(WeaponItem), "GetRecoilTotal")]
    public static class NoRecoilPatch
    {
        static bool Prefix(ref float __result)
        {
            if (CheatState.NoRecoil)
            {
                __result = 0f;
                return false;   // 원본 메서드 스킵
            }
            return true;
        }
    }
}
