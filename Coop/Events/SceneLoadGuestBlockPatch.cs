using HarmonyLib;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// [실제 게스트] 투표 승인(Permitted) 없이 게임 씬 직접 로드 차단.
    /// 참고: 게임의 IL2CPP 네이티브 호출 경로는 차단 불가 —
    ///   게스트 씬 차단은 MissionGateBehaviour.OnSceneLoaded + WaveGroupStartPatch 조합으로 처리.
    ///   이 패치는 모드 코드(OnGuestReady 등 관리 코드)의 중복 로드만 차단.
    /// </summary>
    static class SceneLoadGuestBlockPatch
    {
        private static bool IsSystemScene(string name)
            => name.StartsWith("00_") || name.Length == 0;

        [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.LoadScene),
                      new[] { typeof(string) })]
        static class ByName
        {
            static bool Prefix(string sceneName)
            {
                if (IsSystemScene(sceneName)) return true;

                if (CoopState.IsConnected && !CoopState.IsHost)
                {
                    if (MissionState.Status == MissionStatus.Permitted) return true;
                    Plugin.Log.LogWarning(
                        $"[SceneBlock] Guest 직접 씬 로드 차단: '{sceneName}' (Status={MissionState.Status})");
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.LoadScene),
                      new[] { typeof(int) })]
        static class ByIndex
        {
            static bool Prefix(int sceneBuildIndex)
            {
                if (sceneBuildIndex <= 0) return true;

                if (CoopState.IsConnected && !CoopState.IsHost)
                {
                    if (MissionState.Status == MissionStatus.Permitted) return true;
                    Plugin.Log.LogWarning(
                        $"[SceneBlock] Guest 직접 씬 로드 차단: buildIndex={sceneBuildIndex}");
                    return false;
                }

                return true;
            }
        }
    }
}
