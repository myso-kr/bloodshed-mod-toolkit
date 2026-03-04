using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Role
{
    internal sealed class GuestRole : ICoopRole
    {
        public bool IsHost => false;

        public void OnPeerConnected(CSteamID peer)
            => Plugin.Log.LogInfo($"[GuestRole] Peer 연결: {peer}");

        public void OnPeerDisconnected(CSteamID peer)
            => Plugin.Log.LogInfo($"[GuestRole] Peer 연결 끊김: {peer}");

        // Host → Guest: 미션 사전 알림 + 캐릭터 선택 요청
        public void OnMissionBriefingReceived(string sceneName, int buildIndex)
        {
            Plugin.Log.LogInfo($"[GuestRole] MissionBriefing 수신: '{sceneName}' (idx={buildIndex})");
            MissionState.PendingSceneName  = sceneName;
            MissionState.PendingBuildIndex = buildIndex;
            MissionState.SessionState      = CoopSessionState.Briefing;
            NavigateToCharacterSelection();
        }

        // Host → Guest: 씬 로드 허가
        public void OnMissionStartReceived(string sceneName, int buildIndex)
        {
            Plugin.Log.LogInfo($"[GuestRole] MissionStart 수신: '{sceneName}' (idx={buildIndex})");
            MissionState.PendingSceneName  = sceneName;
            MissionState.PendingBuildIndex = buildIndex;
            MissionState.Status            = MissionStatus.Permitted;

            SendGuestReady();

            if (MissionState.GuestCharacterSelected)
            {
                MissionState.GuestCharacterSelected = false;
                Plugin.Log.LogInfo($"[GuestRole] 캐릭터 선택 완료 → 씬 로드: '{sceneName}'");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                // 미선택 → MetaGame에서 선택 후 자동 진입
                Plugin.Log.LogInfo($"[GuestRole] 캐릭터 미선택 → MetaGame으로 이동 (Pending='{sceneName}')");
                NavigateToCharacterSelection();
            }
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // MetaGame → 00_Loading 전환: 세이브·캐릭터 선택 완료 신호
            if (scene.name.StartsWith("00_Loading") &&
                SceneTracker.FromSceneName == MissionState.MetaGameScene)
            {
                MissionState.GuestCharacterSelected = true;
                Plugin.Log.LogInfo("[GuestRole] MetaGame → LoadingScreen 감지 — 캐릭터 선택 완료");
                return;
            }

            // 시스템 씬: 게이트 판단 불필요
            if (scene.buildIndex <= 0 || scene.name.StartsWith("00_")) return;

            // MetaGame
            if (scene.name == MissionState.MetaGameScene)
            {
                if (MissionState.Status == MissionStatus.WaitingForHost)
                    MissionState.Status = MissionStatus.Idle;

                Plugin.Log.LogInfo(
                    $"[GuestRole] MetaGame 도착" +
                    $" | Status={MissionState.Status}" +
                    $" | SessionState={MissionState.SessionState}" +
                    $" | CharSel={MissionState.GuestCharacterSelected}");

                // 캐릭터 선택 화면이 필요한 경우
                if (MissionState.SessionState == CoopSessionState.Briefing
                 || MissionState.Status == MissionStatus.NeedsCharacterSelect
                 || MissionState.Status == MissionStatus.Permitted)
                    TryShowCharacterSelectionUI();

                return;
            }

            // ── 미션 씬 진입 판단 ─────────────────────────────────────────────
            if (MissionState.Status == MissionStatus.Permitted)
            {
                // Host가 지정한 씬과 다를 경우 리다이렉트
                if (MissionState.PendingSceneName.Length > 0 &&
                    scene.name != MissionState.PendingSceneName)
                {
                    Plugin.Log.LogInfo(
                        $"[GuestRole] 씬 불일치 '{scene.name}' ≠ '{MissionState.PendingSceneName}'" +
                        $" — Host 씬으로 리다이렉트");
                    SceneManager.LoadScene(MissionState.PendingSceneName);
                    return;
                }
                MissionState.GuestCharacterSelected = false;
                MissionState.Status       = MissionStatus.Idle;
                MissionState.SessionState = CoopSessionState.InMission;
                Plugin.Log.LogInfo($"[GuestRole] 허가된 씬 진입 완료 '{scene.name}' — Idle");
            }
            else if (MissionState.GuestCharacterSelected)
            {
                MissionState.GuestCharacterSelected = false;
                MissionState.Status       = MissionStatus.Idle;
                MissionState.SessionState = CoopSessionState.InMission;
                Plugin.Log.LogInfo(
                    $"[GuestRole] 세이브·캐릭터 선택 확인 → 씬 진입 허가: '{scene.name}'");
            }
            else
            {
                // 미인증 진입 — MetaGame으로 리다이렉트
                MissionState.PendingSceneName  = scene.name;
                MissionState.PendingBuildIndex = scene.buildIndex;
                MissionState.Status            = MissionStatus.NeedsCharacterSelect;
                Plugin.Log.LogInfo(
                    $"[GuestRole] 미인증 씬 진입 '{scene.name}'" +
                    $" (from='{SceneTracker.FromSceneName}')" +
                    $" — MetaGame으로 리다이렉트");
                SceneManager.LoadScene(MissionState.MetaGameScene);
            }
        }

        public void OnGuestReadyReceived(ulong id) { }  // Guest는 수신하지 않음

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        private static void NavigateToCharacterSelection()
        {
            var current = SceneManager.GetActiveScene();
            if (current.name == MissionState.MetaGameScene)
                TryShowCharacterSelectionUI();
            else
            {
                Plugin.Log.LogInfo($"[GuestRole] MetaGame으로 이동 (현재: '{current.name}')");
                SceneManager.LoadScene(MissionState.MetaGameScene);
            }
        }

        private static void TryShowCharacterSelectionUI()
        {
            var mgr = UnityEngine.Object.FindObjectOfType<MetaGameManager>();
            if (mgr == null)
            {
                Plugin.Log.LogWarning("[GuestRole] MetaGameManager 미발견");
                return;
            }

            Plugin.Log.LogInfo("[GuestRole] MetaGameManager 발견 — 캐릭터 선택 화면으로 이동");
            mgr.goMetaGameMainMenu?.SetActive(false);
            mgr.goMetaGameEpisodeSelection?.SetActive(false);
            mgr.goMetaGameMissionSelection?.SetActive(false);
            mgr.goMetaGameMissionStart?.SetActive(false);

            if (mgr.goMetaGameCharacterSelection != null)
            {
                mgr.goMetaGameCharacterSelection.SetActive(true);
                Plugin.Log.LogInfo("[GuestRole] 캐릭터 선택 화면 활성화 완료");
            }
            else
            {
                Plugin.Log.LogWarning("[GuestRole] goMetaGameCharacterSelection null");
            }
        }

        private static void SendGuestReady()
        {
            var net = NetManager.Instance;
            if (net == null) return;
            foreach (var peer in CoopState.Peers)
                net.SendReliable(peer, PlayerReadyPacket.Encode());
        }

        private static bool IsMissionScene(Scene s)
            => s.buildIndex > 0
            && !s.name.StartsWith("00_")
            && s.name != MissionState.MetaGameScene;
    }
}
