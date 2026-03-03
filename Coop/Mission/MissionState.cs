using System.Collections.Generic;

namespace BloodshedModToolkit.Coop.Mission
{
    public enum MissionStatus { Idle, WaitingForHost, ReadyUp, Permitted }

    public static class MissionState
    {
        public static MissionStatus Status          { get; set; } = MissionStatus.Idle;
        public static string        PendingSceneName  { get; set; } = "";
        public static int           PendingBuildIndex { get; set; } = -1;
        public static float         ReadyCountdown    { get; set; } = 30f;

        // Host: 게스트별 준비 상태 (ulong = SteamID)
        public static Dictionary<ulong, bool> GuestReadyMap { get; } = new();

        public static void Reset()
        {
            Status            = MissionStatus.Idle;
            PendingSceneName  = "";
            PendingBuildIndex = -1;
            ReadyCountdown    = 30f;
            GuestReadyMap.Clear();
        }
    }
}
