using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Sync
{
    public static class MissionSyncHandler
    {
        // Guest에서 호출 — MissionStart 패킷 수신 시 → 즉시 씬 진입
        public static void OnMissionStart(string sceneName, int buildIndex)
        {
            MissionState.PendingSceneName  = sceneName;
            MissionState.PendingBuildIndex = buildIndex;
            MissionState.Status            = MissionStatus.Permitted;

            // Host에게 준비 완료 전송
            var net = Net.NetManager.Instance;
            if (net != null)
                foreach (var peer in CoopState.Peers)
                    net.SendReliable(peer, Net.PlayerReadyPacket.Encode());

            // 현재 씬과 다른 경우에만 로드
            // MetaGame 중에는 강제 이동 금지 — 게스트가 캐릭터 선택 후 자연스럽게 진입
            var current = SceneManager.GetActiveScene();
            bool alreadyInScene = current.name == sceneName
                                || current.buildIndex == buildIndex;
            bool inMetaGame = current.name == "MetaGame";
            if (!alreadyInScene && !inMetaGame)
            {
                if (sceneName.Length > 0)
                    SceneManager.LoadScene(sceneName);
                else if (buildIndex > 0)
                    SceneManager.LoadScene(buildIndex);
            }

            if (inMetaGame && !alreadyInScene)
                Plugin.Log.LogInfo($"[MissionGate] MetaGame 중 MissionStart 수신 '{sceneName}' → 캐릭터 선택 후 자연 진입 대기");
            else
                Plugin.Log.LogInfo($"[MissionGate] MissionStart 수신 → 진입: {sceneName}");
        }

        // Host에서 호출 — PlayerReady 패킷 수신 시
        public static void OnPlayerReady(ulong from)
        {
            MissionState.GuestReadyMap[from] = true;
            int r = 0;
            foreach (var v in MissionState.GuestReadyMap.Values) if (v) r++;
            Plugin.Log.LogInfo($"[MissionGate] Guest ready: {r}/{CoopState.Peers.Count}");
        }
    }
}
