using System.Collections.Generic;

namespace BloodshedModToolkit.Coop.Mission
{
    public enum MissionStatus
    {
        Idle,
        WaitingForHost,
        Permitted,
    }

    public static class MissionState
    {
        public static MissionStatus Status            { get; set; } = MissionStatus.Idle;
        public static string        PendingSceneName  { get; set; } = "";
        public static int           PendingBuildIndex { get; set; } = -1;

        // Host: 현재 실행 중인 미션 씬 (뒤늦게 접속한 게스트에게 재전송용)
        public static string HostCurrentScene      { get; set; } = "";
        public static int    HostCurrentBuildIndex { get; set; } = -1;

        // Host: 게스트별 입장 확인 (로깅용)
        public static Dictionary<ulong, bool> GuestReadyMap { get; } = new();

        public static void Reset()
        {
            Status                = MissionStatus.Idle;
            PendingSceneName      = "";
            PendingBuildIndex     = -1;
            HostCurrentScene      = "";
            HostCurrentBuildIndex = -1;
            GuestReadyMap.Clear();
        }
    }
}
