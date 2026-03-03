using HarmonyLib;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// [실제 게스트] 투표 승인(Permitted) 없이 게임 씬 직접 로드 차단.
    /// [Debug 게스트 모드] 호스트가 게임 씬 로드 시도 → 차단 후 투표 UI 활성화.
    ///   MissionSyncHandler.OnGuestReady()는 Permitted 설정 → LoadScene 재호출 → 허용.
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

                // ── 실제 게스트: Permitted 아니면 차단 ──────────────────────
                if (CoopState.IsConnected && !CoopState.IsHost)
                {
                    if (MissionState.Status == MissionStatus.Permitted) return true;
                    Plugin.Log.LogWarning(
                        $"[SceneBlock] Guest 직접 씬 로드 차단: '{sceneName}' (Status={MissionState.Status})");
                    return false;
                }

                // ── Debug 게스트 모드: 차단 + 투표 UI 활성화 ──────────────
                if (CoopState.DebugGuestMode)
                {
                    if (MissionState.Status == MissionStatus.Permitted) return true;

                    // 투표가 아직 시작 안 됐을 때만 상태 전환
                    if (MissionState.Status != MissionStatus.VoteRequested &&
                        MissionState.Status != MissionStatus.VoteAccepted)
                    {
                        MissionState.PendingSceneName  = sceneName;
                        MissionState.PendingBuildIndex = -1;
                        MissionState.Status            = MissionStatus.VoteRequested;
                        Plugin.Log.LogInfo(
                            $"[SceneBlock] Debug 모드 차단 → 투표 UI 활성: '{sceneName}'");
                    }
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

                // ── 실제 게스트 ──────────────────────────────────────────────
                if (CoopState.IsConnected && !CoopState.IsHost)
                {
                    if (MissionState.Status == MissionStatus.Permitted) return true;
                    Plugin.Log.LogWarning(
                        $"[SceneBlock] Guest 직접 씬 로드 차단: buildIndex={sceneBuildIndex}");
                    return false;
                }

                // ── Debug 게스트 모드 ─────────────────────────────────────────
                if (CoopState.DebugGuestMode)
                {
                    if (MissionState.Status == MissionStatus.Permitted) return true;

                    if (MissionState.Status != MissionStatus.VoteRequested &&
                        MissionState.Status != MissionStatus.VoteAccepted)
                    {
                        MissionState.PendingSceneName  = "";
                        MissionState.PendingBuildIndex = sceneBuildIndex;
                        MissionState.Status            = MissionStatus.VoteRequested;
                        Plugin.Log.LogInfo(
                            $"[SceneBlock] Debug 모드 차단 → 투표 UI 활성: buildIndex={sceneBuildIndex}");
                    }
                    return false;
                }

                return true;
            }
        }
    }
}
