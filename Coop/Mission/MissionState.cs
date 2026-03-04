using System.Collections.Generic;

namespace BloodshedModToolkit.Coop.Mission
{
    public enum CoopSessionState
    {
        Disconnected,   // 로비 없음
        InLobby,        // 로비 참가, 미션 미시작
        Briefing,       // MissionBriefing 발송/수신 — 캐릭터 선택 진행 중
        InMission,      // 미션 씬 진행 중
    }

    public enum MissionStatus
    {
        Idle,
        WaitingForHost,
        /// <summary>
        /// Guest가 캐릭터 선택 없이 미션 씬에 진입했음 — MetaGame으로 리다이렉트된 상태.
        /// MetaGame에서 캐릭터를 선택하고 게임을 시작하면 다음 씬 진입이 허가된다.
        /// </summary>
        NeedsCharacterSelect,
        Permitted,
    }

    public static class MissionState
    {
        /// <summary>캐릭터 선택·미션 준비 화면 씬 이름 — 게스트가 자유롭게 이용하는 씬.</summary>
        public const string MetaGameScene = "MetaGame";

        public static CoopSessionState SessionState   { get; set; } = CoopSessionState.Disconnected;
        public static MissionStatus Status            { get; set; } = MissionStatus.Idle;
        public static string        PendingSceneName  { get; set; } = "";
        public static int           PendingBuildIndex { get; set; } = -1;

        // Host: 현재 실행 중인 미션 씬 (뒤늦게 접속한 게스트에게 재전송용)
        public static string HostCurrentScene      { get; set; } = "";
        public static int    HostCurrentBuildIndex { get; set; } = -1;

        // Host: 게스트별 입장 확인 (로깅용)
        public static Dictionary<ulong, bool> GuestReadyMap { get; } = new();

        /// <summary>
        /// Guest가 MetaGame → LoadingScreen 전환을 완료했음을 나타내는 플래그.
        /// 세이브 파일 선택 + 캐릭터 선택 + Start 클릭이 모두 완료된 경우에만 true가 된다.
        /// 미션 씬 진입 시 소비(false로 리셋)된다.
        /// </summary>
        public static bool GuestCharacterSelected { get; set; }

        public static void Reset()
        {
            SessionState          = CoopSessionState.Disconnected;
            Status                = MissionStatus.Idle;
            PendingSceneName      = "";
            PendingBuildIndex     = -1;
            HostCurrentScene      = "";
            HostCurrentBuildIndex = -1;
            GuestCharacterSelected = false;
            GuestReadyMap.Clear();
            ItemSelectState.Reset();
            SceneTracker.Reset();
        }
    }
}
