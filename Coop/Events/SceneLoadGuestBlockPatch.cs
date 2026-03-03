using HarmonyLib;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// 연결된 게스트가 투표 승인 없이 게임 씬을 직접 로드하는 것을 차단.
    /// MissionSyncHandler.OnGuestReady()가 Status = Permitted 설정 후 LoadScene을 호출하므로
    /// Permitted 상태에서는 허용된다.
    /// </summary>
    static class SceneLoadGuestBlockPatch
    {
        // 씬 이름이 시스템 씬(메뉴/로딩)이면 true
        private static bool IsSystemScene(string name)
            => name.StartsWith("00_") || name.Length == 0;

        private static bool ShouldBlock(string sceneName)
        {
            if (!CoopState.IsConnected || CoopState.IsHost) return false;
            if (IsSystemScene(sceneName)) return false;
            if (MissionState.Status == MissionStatus.Permitted) return false;
            Plugin.Log.LogWarning(
                $"[SceneBlock] Guest 직접 씬 로드 차단: '{sceneName}' (Status={MissionState.Status})");
            return true;
        }

        [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.LoadScene),
                      new[] { typeof(string) })]
        static class ByName
        {
            static bool Prefix(string sceneName) => !ShouldBlock(sceneName);
        }

        [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.LoadScene),
                      new[] { typeof(int) })]
        static class ByIndex
        {
            static bool Prefix(int sceneBuildIndex)
            {
                if (sceneBuildIndex <= 0) return true;  // 시스템 씬 (buildIndex 0)
                if (!CoopState.IsConnected || CoopState.IsHost) return true;
                if (MissionState.Status == MissionStatus.Permitted) return true;
                Plugin.Log.LogWarning(
                    $"[SceneBlock] Guest 직접 씬 로드 차단: buildIndex={sceneBuildIndex} (Status={MissionState.Status})");
                return false;
            }
        }
    }
}
