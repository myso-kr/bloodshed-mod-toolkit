using System.Collections.Generic;
using Steamworks;
using BloodshedModToolkit.Coop.Ecs;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop
{
    public static class CoopState
    {
        public static bool IsEnabled      { get; set; }
        public static bool IsHost         { get; set; }
        public static bool IsConnected    { get; set; }

        /// <summary>
        /// 호스트가 게스트 플로우를 로컬에서 디버깅하기 위한 모드.
        /// 활성화 시 SceneLoadGuestBlockPatch가 씬 로드를 가로채 투표 UI를 표시하고,
        /// CheatMenu MISSION GATE가 게스트 뷰로 전환된다.
        /// </summary>
        public static bool DebugGuestMode { get; set; }

        public static CSteamID      LobbyId { get; set; } = CSteamID.Nil;
        public static List<CSteamID> Peers  { get; }      = new();

        public const string CoopVersion = "1.0.0";

        public static void Reset()
        {
            IsEnabled   = false;
            IsHost      = false;
            IsConnected = false;
            LobbyId     = CSteamID.Nil;
            Peers.Clear();
            EntityRegistry.Reset();
            MissionState.Reset();
        }
    }
}
