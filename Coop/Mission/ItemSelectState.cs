namespace BloodshedModToolkit.Coop.Mission
{
    /// <summary>
    /// Guest가 Host의 레벨업 카드 선택을 대기하는 상태 관리.
    /// LevelUpScreenManager.OpenLevelUpScreen Postfix에서 IsWaiting = true,
    /// ItemSyncHandler.ApplyItemSelection 완료 시 IsWaiting = false.
    /// </summary>
    public static class ItemSelectState
    {
        public static bool IsWaiting { get; set; } = false;

        /// <summary>레벨업 화면 진입 시 호출 — Guest일 때만 대기 상태 진입.</summary>
        public static void OnSelectionScreenOpened()
        {
            if (CoopState.IsConnected && !CoopState.IsHost)
                IsWaiting = true;
        }

        public static void Reset() => IsWaiting = false;
    }
}
