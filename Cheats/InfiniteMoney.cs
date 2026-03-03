using HarmonyLib;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// 런 중 Gems: PlayerStats.SetMoney Prefix —
    /// 돈이 GemsFloor 이하로 내려가지 않도록 차단합니다.
    /// PersistentData.currentMoney (지속 젬) 는 CheatMenu.Update 에서 직접 설정합니다.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "SetMoney")]
    public static class InfiniteGemsPatch
    {
        static bool Prefix(ref float amount)
        {
            if (CheatState.InfiniteGems && amount < CheatState.GemsFloor)
            {
                Plugin.Log.LogDebug($"[InfiniteGems] SetMoney({amount:F0}) → 강제 {CheatState.GemsFloor:F0}");
                amount = CheatState.GemsFloor;
            }
            return true;
        }
    }
}
