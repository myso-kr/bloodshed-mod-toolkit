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
        /// <summary>캐릭터 선택·미션 준비 화면 씬 이름 — 게스트가 자유롭게 이용하는 씬.</summary>
        public const string MetaGameScene = "MetaGame";

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
