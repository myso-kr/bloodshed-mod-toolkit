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
            MissionState.ReadyCountdown -= Time.deltaTime;
            if (MissionState.ReadyCountdown <= 0f)
                Sync.MissionSyncHandler.OnGuestReady();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!CoopState.IsEnabled) return;

            if (CoopState.IsHost && CoopState.IsConnected)
            {
                // buildIndex 0 = 메인 메뉴/로비 씬 — 미션 씬만 브로드캐스트
                if (scene.buildIndex <= 0) return;
                MissionState.GuestReadyMap.Clear();
                Events.EventBridge.OnMissionStart(scene.name, scene.buildIndex);
                return;
            }

            if (!CoopState.IsHost)
            {
                if (MissionState.Status == MissionStatus.Permitted)
                {
                    MissionState.Status = MissionStatus.Idle;
                }
                else
                {
                    MissionState.Status            = MissionStatus.WaitingForHost;
                    MissionState.PendingSceneName  = scene.name;
                    MissionState.PendingBuildIndex = scene.buildIndex;
                }
            }
        }
    }
}
