using Steamworks;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Events;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Role
{
    internal sealed class HostRole : ICoopRole
    {
        public bool IsHost => true;

        public void OnPeerConnected(CSteamID peer)
            => Plugin.Log.LogInfo($"[HostRole] Peer 연결: {peer}");

        public void OnPeerDisconnected(CSteamID peer)
            => Plugin.Log.LogInfo($"[HostRole] Peer 연결 끊김: {peer}");

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsMissionScene(scene))
            {
                MissionState.GuestReadyMap.Clear();
                MissionState.HostCurrentScene      = scene.name;
                MissionState.HostCurrentBuildIndex = scene.buildIndex;
                MissionState.SessionState          = CoopSessionState.InMission;

                Plugin.Log.LogInfo(
                    $"[HostRole] 미션 진입: '{scene.name}'" +
                    $" — 게스트 {CoopState.Peers.Count}명에게 MissionStart 알림");
                EventBridge.OnMissionStart(scene.name, scene.buildIndex);
            }
            else if (scene.name == MissionState.MetaGameScene &&
                     !string.IsNullOrEmpty(MissionState.HostCurrentScene))
            {
                // 미션 종료 감지: 미션 씬에 있다가 MetaGame으로 복귀
                Plugin.Log.LogInfo(
                    $"[HostRole] 미션 종료 감지: '{MissionState.HostCurrentScene}' → MetaGame" +
                    $" — 게스트 {CoopState.Peers.Count}명에게 MissionEnd 알림");
                EventBridge.OnMissionEnd(success: true);
                MissionState.HostCurrentScene      = "";
                MissionState.HostCurrentBuildIndex = -1;
                MissionState.SessionState          = CoopSessionState.InLobby;
            }
        }

        // Host에서는 no-op
        public void OnMissionBriefingReceived(string s, int i) { }
        public void OnMissionStartReceived(string s, int i)    { }
        public void OnMissionEndReceived(bool success)         { }

        public void OnGuestReadyReceived(ulong guestSteamId)
        {
            MissionState.GuestReadyMap[guestSteamId] = true;
            int r = 0;
            foreach (var v in MissionState.GuestReadyMap.Values) if (v) r++;
            Plugin.Log.LogInfo($"[HostRole] GuestReady: {r}/{CoopState.Peers.Count} ({guestSteamId})");
        }

        /// <summary>
        /// UI에서 미션 선택 완료 시 호출 — Guest에게 사전 알림 + 캐릭터 선택 유도.
        /// </summary>
        public void BroadcastMissionBriefing(string sceneName, int buildIndex)
        {
            MissionState.PendingSceneName  = sceneName;
            MissionState.PendingBuildIndex = buildIndex;
            MissionState.SessionState      = CoopSessionState.Briefing;
            EventBridge.OnMissionBriefing(sceneName, buildIndex);
        }

        private static bool IsMissionScene(Scene s)
            => s.buildIndex > 0
            && !s.name.StartsWith("00_")
            && s.name != MissionState.MetaGameScene;
    }
}
