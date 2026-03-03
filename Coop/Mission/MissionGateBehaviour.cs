using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BloodshedModToolkit.Coop.Mission
{
    public class MissionGateBehaviour : MonoBehaviour
    {
        public MissionGateBehaviour(IntPtr ptr) : base(ptr) { }
        public static MissionGateBehaviour? Instance { get; private set; }

        // 반드시 필드에 보관 — GC 방지 (IL2CPP에서 Action → UnityAction 암묵적 변환)
        private Action<Scene, LoadSceneMode>? _onSceneLoaded;

        void Awake()
        {
            Instance = this;
            _onSceneLoaded = OnSceneLoaded;
            SceneManager.sceneLoaded += _onSceneLoaded;
            Plugin.Log.LogInfo("[MissionGate] 초기화 완료 — sceneLoaded 구독");
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= _onSceneLoaded;
            _onSceneLoaded = null;
            Instance = null;
        }

        void Update()
        {
            if (MissionState.Status != MissionStatus.ReadyUp) return;
            float prev = MissionState.ReadyCountdown;
            MissionState.ReadyCountdown -= Time.deltaTime;
            // 5초 단위로 카운트다운 로그
            if ((int)prev != (int)MissionState.ReadyCountdown
                && (int)MissionState.ReadyCountdown % 5 == 0)
                Plugin.Log.LogInfo($"[MissionGate] Ready-Up 카운트다운: {(int)MissionState.ReadyCountdown}초");
            if (MissionState.ReadyCountdown <= 0f)
                Sync.MissionSyncHandler.OnGuestReady();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 씬 전환은 항상 로그 (Co-op 미사용 시에도 개발 중 추적용)
            Plugin.Log.LogInfo(
                $"[MissionGate] 씬 로드: '{scene.name}' (idx={scene.buildIndex})" +
                $" | Enabled={CoopState.IsEnabled} Host={CoopState.IsHost}" +
                $" Connected={CoopState.IsConnected} Peers={CoopState.Peers.Count}");

            if (!CoopState.IsEnabled) return;

            if (CoopState.IsHost)
            {
                // "00_" 접두사 = 시스템 씬 (Startup/MainMenu/LoadingScreen) — 미션 아님
                if (scene.buildIndex <= 0 || scene.name.StartsWith("00_"))
                {
                    Plugin.Log.LogInfo($"[MissionGate] 시스템 씬 '{scene.name}' — MissionStart 생략");
                    // Debug 게스트 모드: 메뉴 복귀 후 투표 상태 초기화
                    if (CoopState.DebugGuestMode) MissionState.Status = MissionStatus.Idle;
                    return;
                }

                // ── Debug 게스트 모드 ────────────────────────────────────────────
                // SceneManager.LoadScene은 native IL2CPP 경로로 호출되어 HarmonyX 차단 불가.
                // 대신 sceneLoaded 이벤트(항상 발행됨)에서 Status를 VoteRequested로 설정하고
                // WaveGroupStartPatch(DebugGuestMode 경로)가 웨이브를 차단한다.
                if (CoopState.DebugGuestMode)
                {
                    MissionState.PendingSceneName  = scene.name;
                    MissionState.PendingBuildIndex = scene.buildIndex;

                    if (MissionState.Status == MissionStatus.Permitted)
                    {
                        // ACCEPT 후 씬 재진입 완료 → 초기화
                        MissionState.Status = MissionStatus.Idle;
                        Plugin.Log.LogInfo(
                            $"[MissionGate] Debug 모드 — 투표 수락 후 씬 완료: '{scene.name}'");
                    }
                    else
                    {
                        // 투표 미완료 상태에서 씬 진입 → 투표 UI 활성화 + 웨이브 차단
                        MissionState.Status = MissionStatus.VoteRequested;
                        Plugin.Log.LogInfo(
                            $"[MissionGate] Debug 모드 — 씬 진입 감지, 투표 UI 활성: '{scene.name}'");
                    }
                    return;  // MissionStart 브로드캐스트 없음
                }

                // ── 일반 Host 경로 ───────────────────────────────────────────────
                MissionState.GuestReadyMap.Clear();
                MissionState.HostCurrentScene      = scene.name;
                MissionState.HostCurrentBuildIndex = scene.buildIndex;
                Plugin.Log.LogInfo(
                    $"[MissionGate] Host 미션 진입: '{scene.name}'" +
                    $" — 게스트 {CoopState.Peers.Count}명에게 알림");
                Events.EventBridge.OnMissionStart(scene.name, scene.buildIndex);
                return;
            }

            // Guest 경로 — 시스템 씬은 무시
            if (scene.buildIndex <= 0 || scene.name.StartsWith("00_")) return;

            if (MissionState.Status == MissionStatus.Permitted)
            {
                MissionState.Status = MissionStatus.Idle;
                Plugin.Log.LogInfo($"[MissionGate] 허가된 씬 진입 완료 '{scene.name}' — 상태 Idle");
            }
            else
            {
                MissionState.Status            = MissionStatus.WaitingForHost;
                MissionState.PendingSceneName  = scene.name;
                MissionState.PendingBuildIndex = scene.buildIndex;
                Plugin.Log.LogInfo($"[MissionGate] Guest 독립 씬 진입 — WaitingForHost: '{scene.name}'");
            }
        }
    }
}
