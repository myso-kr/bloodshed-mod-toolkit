using System.Collections.Generic;

namespace BloodshedModToolkit.Coop.Mission
{
    public enum MissionStatus
    {
        Idle,
        WaitingForHost,
        ReadyUp,
        Permitted,
        VoteRequested,  // Phase 11: Host가 투표 요청, Guest 응답 대기 중
        VoteAccepted,   // Phase 11: Guest가 수락 완료, Host 게임 시작 대기 중
    }

    public static class MissionState
    {
        public static MissionStatus Status          { get; set; } = MissionStatus.Idle;
        public static string        PendingSceneName  { get; set; } = "";
        public static int           PendingBuildIndex { get; set; } = -1;
        public static float         ReadyCountdown    { get; set; } = 30f;
        public static float         VoteCountdown     { get; set; } = 30f;

        // Host: 게스트별 준비 상태 (ulong = SteamID)
        public static Dictionary<ulong, bool> GuestReadyMap { get; } = new();

        // Host: 현재 실행 중인 미션 씬 (뒤늦게 접속한 게스트에게 재전송용)
        public static string HostCurrentScene      { get; set; } = "";
        public static int    HostCurrentBuildIndex { get; set; } = -1;

        // Phase 11: Host 투표 상태
        public static bool HostVoteActive { get; set; }
        // key=SteamID, value=true(수락)/false(거부). 미응답은 키 없음.
        public static Dictionary<ulong, bool> VoteAcceptMap { get; } = new();

        public static void Reset()
        {
            Status               = MissionStatus.Idle;
            PendingSceneName     = "";
            PendingBuildIndex    = -1;
            ReadyCountdown       = 30f;
            VoteCountdown        = 30f;
            HostCurrentScene     = "";
            HostCurrentBuildIndex = -1;
            GuestReadyMap.Clear();
            HostVoteActive = false;
            VoteAcceptMap.Clear();
        }
    }
}
