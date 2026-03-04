namespace BloodshedModToolkit.Coop.Sync
{
    public static class MissionSyncHandler
    {
        // CoopSessionManager(GuestRole)로 위임
        public static void OnMissionStart(string sceneName, int buildIndex)
            => CoopSessionManager.NotifyMissionStart(sceneName, buildIndex);

        // CoopSessionManager(HostRole)로 위임
        public static void OnPlayerReady(ulong from)
            => CoopSessionManager.NotifyGuestReady(from);
    }
}
