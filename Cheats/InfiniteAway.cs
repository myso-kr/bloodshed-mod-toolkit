using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 무한 추방기 (Infinite Away / Exile) 구현.
    ///
    /// "추방기"는 레벨업 화면에서 아이템을 영구 제거하는 Away 기능입니다.
    /// PlayerInventory.HasLevelUpAway() 를 후킹해 항상 사용 가능 상태로 만들고,
    /// PlayerStats.LevelUpAway 도 지속적으로 높은 값으로 유지합니다.
    /// </summary>
    [HarmonyPatch(typeof(PlayerInventory), "HasLevelUpAway")]
    public static class InfiniteAwayPatch
    {
        /// <summary>
        /// Prefix: InfiniteAway 가 켜져 있으면 __result = true 를 반환하고 원본 스킵.
        /// → 추방기가 없어도 항상 "있다"고 판단됩니다.
        /// </summary>
        static bool Prefix(ref bool __result)
        {
            if (CheatState.InfiniteAway)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
