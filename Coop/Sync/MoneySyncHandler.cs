using com8com1.SCFPS;

namespace BloodshedModToolkit.Coop.Sync
{
    /// <summary>
    /// Co-op 젬(money) 동기화 로직.
    /// MoneyEventPatch에서 호출됩니다.
    /// </summary>
    public static class MoneySyncHandler
    {
        // 재귀 방지 플래그
        private static bool _applyingRemote = false;
        public static bool IsApplyingRemote => _applyingRemote;

        public static void ApplyDelta(PlayerStats ps, float delta)
        {
            _applyingRemote = true;
            try   { ps.SetMoney(ps.money + delta); }
            finally { _applyingRemote = false; }
        }
    }
}
