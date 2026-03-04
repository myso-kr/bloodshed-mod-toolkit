using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Sync
{
    /// <summary>
    /// Guest 측에서 Host의 아이템/카드 선택을 자동 적용.
    /// IsApplyingRemoteItem 플래그로 ItemSelectedEventPatch 재귀 방지.
    /// </summary>
    public static class ItemSyncHandler
    {
        private static bool _applying = false;
        public  static bool IsApplyingRemoteItem => _applying;

        public static void WithApplyingGuard(System.Action action)
        {
            _applying = true;
            try   { action(); }
            finally { _applying = false; }
        }

        public static void ApplyItemSelection(int cardIndex)
        {
            var inv = Object.FindObjectOfType<PlayerInventory>();
            if (inv == null)
            {
                Plugin.Log.LogWarning("[ItemSync] PlayerInventory 없음 — 스킵");
                return;
            }

            var options = inv.upgradeUiOptions;
            if (options == null || cardIndex < 0 || cardIndex >= options.Count)
            {
                Plugin.Log.LogWarning(
                    $"[ItemSync] 유효하지 않은 인덱스 {cardIndex} " +
                    $"(options={options?.Count.ToString() ?? "null"})");
                return;
            }

            var ui = options[cardIndex];
            if (ui?.upgradeButton == null)
            {
                Plugin.Log.LogWarning("[ItemSync] upgradeButton 없음 — 스킵");
                return;
            }

            WithApplyingGuard(() =>
            {
                ui.upgradeButton.onClick.Invoke();
                ItemSelectState.IsWaiting = false;
            });
            Plugin.Log.LogInfo($"[ItemSync] Guest 자동 선택: index={cardIndex}");
        }
    }
}
