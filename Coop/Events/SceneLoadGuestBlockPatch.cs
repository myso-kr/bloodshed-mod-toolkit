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
        // MetaGame은 캐릭터 선택 화면이므로 항상 허용 (NeedsCharacterSelect 리다이렉트용)
        private static bool IsAllowedScene(string name)
            => name.StartsWith("00_") || name.Length == 0 || name == MissionState.MetaGameScene;

        [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.LoadScene),
                      new[] { typeof(string) })]
        static class ByName
        {
            static bool Prefix(string sceneName)
            {
                if (IsAllowedScene(sceneName)) return true;

                if (CoopState.IsConnected && !CoopState.IsHost)
                {
                    if (MissionState.Status == MissionStatus.Permitted ||
                        MissionState.Status == MissionStatus.NeedsCharacterSelect) return true;
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
                    if (MissionState.Status == MissionStatus.Permitted ||
                        MissionState.Status == MissionStatus.NeedsCharacterSelect) return true;
                    Plugin.Log.LogWarning(
                        $"[SceneBlock] Guest 직접 씬 로드 차단: buildIndex={sceneBuildIndex}");
                    return false;
                }

                return true;
            }
        }
    }
}
