using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BloodshedModToolkit
{
    /// <summary>
    /// 백그라운드에서 GitHub Releases API를 조회해 최신 버전을 확인합니다.
    /// 실패 시 무시하며, 업데이트 알림은 non-fatal입니다.
    /// </summary>
    internal static class UpdateChecker
    {
        private const string ApiUrl =
            "https://api.github.com/repos/myso-kr/bloodshed-mod-toolkit/releases/latest";

        /// <summary>
        /// 백그라운드 조회 완료 후 채워지는 최신 버전 문자열 (예: "1.0.163").
        /// null 이면 아직 조회 중이거나 실패한 것입니다.
        /// </summary>
        public static volatile string? LatestVersion;

        /// <summary>
        /// 현재 플러그인 버전보다 새 버전이 존재하면 true.
        /// </summary>
        public static bool IsOutdated
            => LatestVersion != null && LatestVersion != MyPluginInfo.PLUGIN_VERSION;

        /// <summary>
        /// 백그라운드 Task 로 GitHub API 를 호출합니다. 호출 후 즉시 반환됩니다.
        /// </summary>
        public static void StartAsync()
        {
            Task.Run(async () =>
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "BloodshedModToolkit");
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var json = await client.GetStringAsync(ApiUrl).ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("tag_name", out var tagEl))
                    {
                        var tag = tagEl.GetString() ?? string.Empty;
                        LatestVersion = tag.TrimStart('v');
                    }
                }
                catch
                {
                    // 네트워크 오류 등 — 무시 (업데이트 알림 미표시)
                }
            });
        }
    }
}
