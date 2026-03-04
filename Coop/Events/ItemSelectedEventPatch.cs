using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// Host의 레벨업 카드 선택을 감지해 Guest에게 브로드캐스트.
    /// PlayerInventory의 모든 레벨업 메뉴 선택 메서드를 공통으로 패치.
    /// </summary>
    [HarmonyPatch]
    public static class ItemSelectedEventPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var names = new[]
            {
                "AddFromLevelUpMenu",
                "LevelUpWeaponFromLevelUpMenu",
                "LevelUpActiveItemFromLevelUpMenu",
                "LevelUpPassiveItemFromLevelUpMenu",
            };
            foreach (var name in names)
            {
                var m = AccessTools.Method(typeof(PlayerInventory), name);
                if (m != null) yield return m;
            }
        }

        static void Postfix(int upgradeOptionIndex)
        {
            if (ItemSyncHandler.IsApplyingRemoteItem) return;
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            EventBridge.OnItemSelected(upgradeOptionIndex);
            Plugin.Log.LogInfo($"[ItemSelected] Host 선택: index={upgradeOptionIndex}");
        }
    }

    /// <summary>레벨업 화면 열릴 때 Guest 대기 상태 진입.</summary>
    [HarmonyPatch(typeof(LevelUpScreenManager), "OpenLevelUpScreen")]
    public static class LevelUpScreenOpenPatch
    {
        static void Postfix() => ItemSelectState.OnSelectionScreenOpened();
    }

    /// <summary>레벨업 화면 닫힐 때 대기 상태 해제 (안전 클리어).</summary>
    [HarmonyPatch(typeof(LevelUpScreenManager), "CloseLevelUpScreen")]
    public static class LevelUpScreenClosePatch
    {
        static void Postfix() => ItemSelectState.Reset();
    }
}
