using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Sync
{
    public static class MissionSyncHandler
    {
        // Guest에서 호출 — MissionStart 패킷 수신 시
        public static void OnMissionStart(string sceneName, int buildIndex)
        {
            MissionState.PendingSceneName  = sceneName;
            MissionState.PendingBuildIndex = buildIndex;
            MissionState.ReadyCountdown    = 30f;
            MissionState.Status            = MissionStatus.ReadyUp;
            Plugin.Log.LogInfo($"[MissionGate] Host started mission: {sceneName} — 30s Ready-Up");
        }

        // Host에서 호출 — PlayerReady 패킷 수신 시
        public static void OnPlayerReady(ulong from)
        {
            MissionState.GuestReadyMap[from] = true;
            int r = 0;
            foreach (var v in MissionState.GuestReadyMap.Values) if (v) r++;
            Plugin.Log.LogInfo($"[MissionGate] Guest ready: {r}/{CoopState.Peers.Count}");
        }

        // Guest Ready 버튼 클릭 or 타임아웃
        public static void OnGuestReady()
        {
            if (MissionState.Status != MissionStatus.ReadyUp) return;
            MissionState.Status = MissionStatus.Permitted;

            // Host에게 준비 완료 전송
            var net = Net.NetManager.Instance;
            if (net != null && CoopState.Peers.Count > 0)
            {
                foreach (var peer in CoopState.Peers)
                    net.SendReliable(peer, Net.PlayerReadyPacket.Encode());
            }

            // 씬 로드 (아직 해당 씬이 아닌 경우)
            var current = SceneManager.GetActiveScene();
            bool alreadyInScene = current.name == MissionState.PendingSceneName
                                || current.buildIndex == MissionState.PendingBuildIndex;
            if (!alreadyInScene && MissionState.PendingSceneName.Length > 0)
                SceneManager.LoadScene(MissionState.PendingSceneName);

            Plugin.Log.LogInfo("[MissionGate] Guest ready — entering mission");
        }
    }
}
