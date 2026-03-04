using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Renderer;

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

        // ── activeSceneChanged: from/to 추적 ────────────────────────────────
        private void OnActiveSceneChanged(Scene from, Scene to)
            => SceneTracker.OnActiveSceneChanged(from, to);

        // ── sceneUnloaded: 로드 목록 갱신 ───────────────────────────────────
        private void OnSceneUnloaded(Scene scene)
            => SceneTracker.OnSceneUnloaded(scene);

        // ── sceneLoaded: SceneTracker + Role 위임 ───────────────────────────
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
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
                $" Session={MissionState.SessionState}" +
                $" CharSel={MissionState.GuestCharacterSelected}");

            // 실제 미션 씬에서 이펙트 캐시 스캔
            if (scene.buildIndex > 0 && !scene.name.StartsWith("00_") && scene.name != "MetaGame")
                EffectCache.ScanScene();

            if (!CoopState.IsEnabled) return;
            CoopSessionManager.NotifySceneLoaded(scene, mode);
        }
    }
}
