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

            // 씬 진입 처리
            var current = SceneManager.GetActiveScene();
            bool alreadyInScene = current.name == sceneName
                                || current.buildIndex == buildIndex;
            bool inMetaGame = current.name == Mission.MissionState.MetaGameScene;

            if (alreadyInScene)
            {
                // 이미 목표 씬에 있음 (Guest가 먼저 진입한 경우) — 즉시 Idle 허가
                MissionState.Status = MissionStatus.Idle;
                Plugin.Log.LogInfo($"[MissionGate] MissionStart 수신 — 이미 '{sceneName}' — 즉시 Idle");
            }
            else if (inMetaGame)
            {
                // MetaGame 중: 저장파일·미션지역·캐릭터 선택 완료 후 자연스럽게 Host 씬 진입
                // MissionGateBehaviour.OnSceneLoaded에서 씬 불일치 시 자동 리다이렉트 처리
                Plugin.Log.LogInfo($"[MissionGate] MetaGame 중 MissionStart 수신 '{sceneName}' → 준비 완료 후 진입 대기");
            }
            else
            {
                if (sceneName.Length > 0)
                    SceneManager.LoadScene(sceneName);
                else if (buildIndex > 0)
                    SceneManager.LoadScene(buildIndex);
                Plugin.Log.LogInfo($"[MissionGate] MissionStart 수신 → 진입: {sceneName}");
            }
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
