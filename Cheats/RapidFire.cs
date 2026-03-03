using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 속사 (Rapid Fire) —
    /// ShotAction.SetCooldownEnd Postfix 에서 CooldownEnd 를 0 으로 강제합니다.
    ///
    /// IsOnCooldown 게터는 "Time.time &lt; CooldownEnd" 를 반환하는 tiny getter 이므로
    /// HarmonyX 트램폴린이 적용되지 않습니다.
    /// SetCooldownEnd 는 발사 직후 반드시 호출되는 메서드이며, 여기서 종료 시각을
    /// 0 으로 덮어쓰면 IsOnCooldown 이 항상 false 가 됩니다 (Time.time > 0 이므로).
    /// </summary>
    [HarmonyPatch(typeof(ShotAction), nameof(ShotAction.SetCooldownEnd))]
    public static class RapidFirePatch
    {
        static void Postfix(ShotAction __instance)
        {
            if (CheatState.RapidFire)
                __instance.CooldownEnd = 0f;
        }
    }
}
