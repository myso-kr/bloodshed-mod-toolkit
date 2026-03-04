using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Coop.Mission
{
    public class MissionGateBehaviour : MonoBehaviour
    {
        public MissionGateBehaviour(IntPtr ptr) : base(ptr) { }
        public static MissionGateBehaviour? Instance { get; private set; }

        // 반드시 필드에 보관 — GC 방지 (IL2CPP에서 Action → UnityAction 암묵적 변환)
        private Action<Scene, LoadSceneMode>? _onSceneLoaded;
        private Action<Scene>?                _onSceneUnloaded;
        private Action<Scene, Scene>?         _onActiveSceneChanged;

        void Awake()
        {
            Instance = this;

            _onSceneLoaded        = OnSceneLoaded;
            _onSceneUnloaded      = OnSceneUnloaded;
            _onActiveSceneChanged = OnActiveSceneChanged;

            SceneManager.sceneLoaded        += _onSceneLoaded;
            SceneManager.sceneUnloaded      += _onSceneUnloaded;
            SceneManager.activeSceneChanged += _onActiveSceneChanged;

            Plugin.Log.LogInfo("[MissionGate] 초기화 완료 — sceneLoaded/Unloaded/activeSceneChanged 구독");
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded        -= _onSceneLoaded;
            SceneManager.sceneUnloaded      -= _onSceneUnloaded;
            SceneManager.activeSceneChanged -= _onActiveSceneChanged;

            _onSceneLoaded        = null;
            _onSceneUnloaded      = null;
            _onActiveSceneChanged = null;
            Instance = null;
        }

        // ── MetaGame 화면 이동 ───────────────────────────────────────────────

        /// <summary>
        /// Guest가 NeedsCharacterSelect 상태로 MetaGame에 도착했을 때,
        /// MetaGameManager를 찾아 캐릭터 선택 화면으로 직접 이동합니다.
        /// sceneLoaded는 씬의 Awake/Start 이후에 발생하므로 즉시 호출 가능.
        /// </summary>
        private void NavigateToCharacterSelection()
        {
            var mgr = UnityEngine.Object.FindObjectOfType<MetaGameManager>();
            if (mgr == null)
            {
                Plugin.Log.LogWarning("[MissionGate] MetaGameManager 미발견 — root GameObject 목록 출력");
                LogMetaGameRootObjects();
                return;
            }

            Plugin.Log.LogInfo("[MissionGate] MetaGameManager 발견 — 캐릭터 선택 화면으로 이동");
            Plugin.Log.LogInfo(
                $"[MissionGate]   MainMenu={mgr.goMetaGameMainMenu?.name ?? "null"}" +
                $" EpisodeSel={mgr.goMetaGameEpisodeSelection?.name ?? "null"}" +
                $" MissionSel={mgr.goMetaGameMissionSelection?.name ?? "null"}" +
                $" CharSel={mgr.goMetaGameCharacterSelection?.name ?? "null"}");

            mgr.goMetaGameMainMenu?.SetActive(false);
            mgr.goMetaGameEpisodeSelection?.SetActive(false);
            mgr.goMetaGameMissionSelection?.SetActive(false);
            mgr.goMetaGameMissionStart?.SetActive(false);

            if (mgr.goMetaGameCharacterSelection != null)
            {
                mgr.goMetaGameCharacterSelection.SetActive(true);
                Plugin.Log.LogInfo("[MissionGate] 캐릭터 선택 화면 활성화 완료");
            }
            else
            {
                Plugin.Log.LogWarning("[MissionGate] goMetaGameCharacterSelection null — root 목록 출력");
                LogMetaGameRootObjects();
            }
        }

        private void LogMetaGameRootObjects()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            Plugin.Log.LogInfo($"[MissionGate] MetaGame root objects ({roots.Length}):");
            foreach (var go in roots)
                Plugin.Log.LogInfo($"[MissionGate]   '{go.name}' active={go.activeSelf}");
        }

        // ── activeSceneChanged: from/to 추적 ────────────────────────────────
        private void OnActiveSceneChanged(Scene from, Scene to)
            => SceneTracker.OnActiveSceneChanged(from, to);

        // ── sceneUnloaded: 로드 목록 갱신 ───────────────────────────────────
        private void OnSceneUnloaded(Scene scene)
            => SceneTracker.OnSceneUnloaded(scene);

        // ── sceneLoaded: 메인 게이트 로직 ───────────────────────────────────
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // SceneTracker 항상 업데이트 (Co-op 미사용 시에도 추적)
            SceneTracker.OnSceneLoaded(scene, mode);

            Plugin.Log.LogInfo(
                $"[MissionGate] ── onSceneLoaded ─────────────────────────");
            Plugin.Log.LogInfo(
                $"[MissionGate]   scene  : '{scene.name}' (idx={scene.buildIndex}, {mode})");
            Plugin.Log.LogInfo(
                $"[MissionGate]   from   : '{SceneTracker.FromSceneName}' (idx={SceneTracker.FromSceneBuildIndex})");
            Plugin.Log.LogInfo(
                $"[MissionGate]   coop   : Enabled={CoopState.IsEnabled} Host={CoopState.IsHost}" +
                $" Connected={CoopState.IsConnected} Peers={CoopState.Peers.Count}");
            Plugin.Log.LogInfo(
                $"[MissionGate]   status : {MissionState.Status}" +
                $" CharSel={MissionState.GuestCharacterSelected}");

            if (!CoopState.IsEnabled) return;

            // ── Host 경로 ────────────────────────────────────────────────────
            if (CoopState.IsHost)
            {
                if (scene.buildIndex <= 0 || scene.name.StartsWith("00_") ||
                    scene.name == MissionState.MetaGameScene)
                {
                    Plugin.Log.LogInfo($"[MissionGate] 시스템/비미션 씬 '{scene.name}' — MissionStart 생략");
                    return;
                }

                MissionState.GuestReadyMap.Clear();
                MissionState.HostCurrentScene      = scene.name;
                MissionState.HostCurrentBuildIndex = scene.buildIndex;
                Plugin.Log.LogInfo(
                    $"[MissionGate] Host 미션 진입: '{scene.name}'" +
                    $" — 게스트 {CoopState.Peers.Count}명에게 알림");
                Events.EventBridge.OnMissionStart(scene.name, scene.buildIndex);
                return;
            }

            // ── Guest 경로 ───────────────────────────────────────────────────

            // 시스템 씬 (00_*): 게이트 판단 불필요, 특수 전환만 체크
            if (scene.buildIndex <= 0 || scene.name.StartsWith("00_"))
            {
                // MetaGame → LoadingScreen 전환 = 세이브+캐릭터 선택 완료 신호
                // 게임이 Start 버튼 처리를 완료한 후에만 이 전환이 발생한다
                if (scene.name.StartsWith("00_Loading") &&
                    SceneTracker.FromSceneName == MissionState.MetaGameScene)
                {
                    MissionState.GuestCharacterSelected = true;
                    Plugin.Log.LogInfo(
                        "[MissionGate] MetaGame → LoadingScreen 전환 감지" +
                        " — 세이브·캐릭터 선택 완료, 미션 씬 진입 허가 마킹");
                }
                return;
            }

            // MetaGame: 캐릭터 선택 준비 화면 — 게스트가 자유롭게 이용
            if (scene.name == MissionState.MetaGameScene)
            {
                if (MissionState.Status == MissionStatus.WaitingForHost)
                    MissionState.Status = MissionStatus.Idle;
                Plugin.Log.LogInfo(
                    $"[MissionGate] Guest MetaGame 도착" +
                    $" | Status={MissionState.Status}" +
                    $" | CharacterSelected={MissionState.GuestCharacterSelected}");

                // NeedsCharacterSelect 상태: 캐릭터 선택 화면으로 바로 이동
                if (MissionState.Status == MissionStatus.NeedsCharacterSelect)
                    NavigateToCharacterSelection();

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
                        $"[MissionGate] 씬 불일치 '{scene.name}' ≠ '{MissionState.PendingSceneName}'" +
                        $" — Host 씬으로 리다이렉트");
                    SceneManager.LoadScene(MissionState.PendingSceneName);
                    return;
                }
                MissionState.GuestCharacterSelected = false;
                MissionState.Status = MissionStatus.Idle;
                Plugin.Log.LogInfo($"[MissionGate] 허가된 씬 진입 완료 '{scene.name}' — Idle");
            }
            else if (MissionState.GuestCharacterSelected)
            {
                // 세이브+캐릭터 선택 확인 완료 → 진입 허가, 플래그 소비
                MissionState.GuestCharacterSelected = false;
                MissionState.Status = MissionStatus.Idle;
                Plugin.Log.LogInfo(
                    $"[MissionGate] Guest 세이브·캐릭터 선택 확인 → 씬 진입 허가: '{scene.name}'");
            }
            else
            {
                // 미인증 진입 (native sync 또는 직접 로드) — MetaGame으로 리다이렉트
                MissionState.PendingSceneName  = scene.name;
                MissionState.PendingBuildIndex = scene.buildIndex;
                MissionState.Status = MissionStatus.NeedsCharacterSelect;
                Plugin.Log.LogInfo(
                    $"[MissionGate] Guest 미인증 씬 진입 '{scene.name}'" +
                    $" (from='{SceneTracker.FromSceneName}')" +
                    $" — MetaGame으로 리다이렉트");
                SceneManager.LoadScene(MissionState.MetaGameScene);
            }
        }
    }
}
