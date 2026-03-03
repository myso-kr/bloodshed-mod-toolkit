using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 장전 없음 (No Reload) —
    /// Weapon.Update 매 프레임 탄창을 최대치로 유지하고,
    /// 진행 중인 장전 코루틴을 중단합니다.
    /// 수동 장전 키(R)를 눌러도 장전 상태가 즉시 취소됩니다.
    /// </summary>
    [HarmonyPatch(typeof(Weapon), "Update")]
    public static class NoReloadPatch
    {
        static void Postfix(Weapon __instance)
        {
            if (!CheatState.NoReload) return;
            var wd = __instance.weaponData;
            if (wd == null || !wd.magazineBased) return;

            // 진행 중인 장전 코루틴 중단
            if (__instance.isReloading)
            {
                var cr = __instance.coroutineReloading;
                if (cr != null) __instance.StopCoroutine(cr);
                __instance.isReloading = false;
            }

            // 탄창 항상 최대로 유지
            __instance.mag = wd.magazineSize;
        }
    }
}
