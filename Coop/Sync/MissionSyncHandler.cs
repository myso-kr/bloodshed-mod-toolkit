using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Mission;
using Steamworks;

namespace BloodshedModToolkit.Coop.Sync
{
    public static class MissionSyncHandler
    {
        // ── 기존: MissionStart / PlayerReady ─────────────────────────────────

        // Guest에서 호출 — MissionStart 패킷 수신 시
        public static void OnMissionStart(string sceneName, int buildIndex)
        {
            MissionState.PendingSceneName  = sceneName;
            MissionState.PendingBuildIndex = buildIndex;

            // Phase 11: 이미 투표 수락한 게스트 → 자동 입장 (READY UP 불필요)
            if (MissionState.Status == MissionStatus.VoteAccepted)
            {
                Plugin.Log.LogInfo($"[MissionGate] 투표 수락 상태 — 자동 입장: {sceneName}");
                OnGuestReady();
                return;
            }

            MissionState.ReadyCountdown = 30f;
            MissionState.Status         = MissionStatus.ReadyUp;
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

        // Guest Ready 버튼 클릭 or 타임아웃 or 자동 진입(VoteAccepted)
        public static void OnGuestReady()
        {
            if (MissionState.Status != MissionStatus.ReadyUp &&
                MissionState.Status != MissionStatus.VoteAccepted) return;
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
            if (!alreadyInScene)
            {
                if (MissionState.PendingSceneName.Length > 0)
                    SceneManager.LoadScene(MissionState.PendingSceneName);
                else if (MissionState.PendingBuildIndex > 0)
                    SceneManager.LoadScene(MissionState.PendingBuildIndex);
            }

            Plugin.Log.LogInfo("[MissionGate] Guest ready — entering mission");
        }

        // ── Phase 11: 투표 기반 게임 시작 ────────────────────────────────────

        // Host에서 호출 — "VOTE: START GAME" 버튼 클릭 시
        public static void OnHostVoteStart()
        {
            if (!CoopState.IsHost) return;
            MissionState.VoteAcceptMap.Clear();
            MissionState.HostVoteActive = true;
            Events.EventBridge.OnVoteStart();
            Plugin.Log.LogInfo($"[Vote] 투표 시작 → 게스트 {CoopState.Peers.Count}명");
        }

        // Guest에서 호출 — VoteStart 패킷 수신 시
        public static void OnVoteStart()
        {
            MissionState.VoteCountdown = 30f;
            MissionState.Status = MissionStatus.VoteRequested;
            Plugin.Log.LogInfo("[Vote] 호스트 게임 시작 투표 수신 — 30초 자동 수락");
        }

        // Guest에서 호출 — ACCEPT 버튼 클릭 or 30초 자동 수락
        public static void OnGuestVoteResponse(bool accepted)
        {
            if (!accepted) return;  // Reject 제거 — 항상 수락

            // Debug 게스트 모드: 네트워크 전송 없이 로컬 상태만 전환
            if (CoopState.DebugGuestMode)
            {
                MissionState.Status = MissionStatus.Permitted;
                Plugin.Log.LogInfo("[Vote] Debug 모드 ACCEPT → Permitted (웨이브 해제)");
                return;
            }

            MissionState.Status = MissionStatus.VoteAccepted;
            Events.EventBridge.OnVoteAccept(true);
            Plugin.Log.LogInfo("[Vote] 수락 전송");
        }

        // Host에서 호출 — VoteAccept 패킷 수신 시
        public static void OnVoteAccept(ulong from, bool accepted)
        {
            MissionState.VoteAcceptMap[from] = accepted;

            int acceptCount = 0;
            foreach (var v in MissionState.VoteAcceptMap.Values) if (v) acceptCount++;

            string name;
            try { name = SteamFriends.GetFriendPersonaName(new CSteamID(from)); }
            catch { name = $"{from:X8}"; }

            Plugin.Log.LogInfo(
                $"[Vote] {name} → {(accepted ? "수락" : "거부")} " +
                $"({acceptCount}/{CoopState.Peers.Count})");

            if (acceptCount >= CoopState.Peers.Count && CoopState.Peers.Count > 0)
            {
                MissionState.HostVoteActive = false;
                Plugin.Log.LogInfo("[Vote] 전원 동의 완료 — 게임 시작 가능");
            }
        }
    }
}
