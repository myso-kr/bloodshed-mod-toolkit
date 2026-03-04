using System.Text.Json.Nodes;

namespace SteamCard;

/// <summary>
/// Bloodshed (App 2747550) 의 실시간 가격/리뷰 데이터를 Steam Web API에서 조회합니다.
/// API 키 불필요.
/// </summary>
public sealed class SteamApiClient : IDisposable
{
    public const int AppId = 2747550;

    private readonly HttpClient _http;

    public SteamApiClient()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 BloodshedModToolkit-SteamCard/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>가격, 할인율, 리뷰 점수, 짧은 설명을 병렬로 조회합니다.</summary>
    public async Task<SteamGameData> FetchAsync(CancellationToken ct = default)
    {
        var detailsTask = FetchAppDetailsAsync(ct);
        var reviewsTask = FetchReviewSummaryAsync(ct);
        await Task.WhenAll(detailsTask, reviewsTask);

        var (name, shortDesc, price, originalPrice, discountPct) = detailsTask.Result;
        var (positive, total)                                     = reviewsTask.Result;

        int reviewPct = total > 0 ? (int)Math.Round(positive * 100.0 / total) : 0;
        string reviewLabel = reviewPct switch
        {
            >= 95 => "Overwhelmingly Positive",
            >= 85 => "Very Positive",
            >= 80 => "Mostly Positive",
            >= 70 => "Positive",
            >= 40 => "Mixed",
            >= 20 => "Mostly Negative",
            _     => "Overwhelmingly Negative"
        };

        return new SteamGameData(name, shortDesc, price, originalPrice, discountPct,
                                 reviewPct, reviewLabel, total);
    }

    // ── appdetails ────────────────────────────────────────────────────────────
    private async Task<(string name, string shortDesc, string price, string originalPrice, int discount)>
        FetchAppDetailsAsync(CancellationToken ct)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={AppId}";
        var json = await GetJsonAsync(url, ct);

        if (json is null) return ("Bloodshed", "", "N/A", "N/A", 0);

        var app = json[$"{AppId}"]?.AsObject();
        if (app is null || !(app["success"]?.GetValue<bool>() ?? false))
            return ("Bloodshed", "", "N/A", "N/A", 0);

        var data = app["data"]?.AsObject();
        if (data is null) return ("Bloodshed", "", "N/A", "N/A", 0);

        string name      = data["name"]?.GetValue<string>()              ?? "Bloodshed";
        string shortDesc = data["short_description"]?.GetValue<string>() ?? "";

        var po = data["price_overview"]?.AsObject();
        if (po is null) return (name, shortDesc, "Free", "Free", 0);

        string finalFmt   = po["final_formatted"]?.GetValue<string>()  ?? "N/A";
        string initialFmt = po["initial_formatted"]?.GetValue<string>() ?? finalFmt;
        int    discount   = po["discount_percent"]?.GetValue<int>()     ?? 0;

        return (name, shortDesc, finalFmt, initialFmt, discount);
    }

    // ── appreviews ────────────────────────────────────────────────────────────
    private async Task<(int positive, int total)> FetchReviewSummaryAsync(CancellationToken ct)
    {
        var url = $"https://store.steampowered.com/appreviews/{AppId}" +
                  $"?json=1&num_per_page=1&language=all&purchase_type=all";
        var json = await GetJsonAsync(url, ct);

        if (json is null) return (0, 0);

        var summary = json["query_summary"]?.AsObject();
        if (summary is null) return (0, 0);

        int positive = summary["total_positive"]?.GetValue<int>() ?? 0;
        int total    = summary["total_reviews"]?.GetValue<int>()  ?? 0;
        return (positive, total);
    }

    /// <summary>JSON을 파싱합니다. HTTP 오류/파싱 실패 시 null 반환 (non-throwing).</summary>
    private async Task<JsonObject?> GetJsonAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(
                    $"[SteamCard] HTTP {(int)response.StatusCode}: {url}");
                return null;
            }
            var text = await response.Content.ReadAsStringAsync(ct);
            return JsonNode.Parse(text)?.AsObject();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SteamCard] 요청 실패 {url}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// capsule_184x69.jpg 다운로드. 없으면 header.jpg → library_hero.jpg 폴백.
    /// 위젯 공식 이미지 (float-left, 184×69).
    /// </summary>
    public async Task<byte[]> DownloadCapsuleSmallAsync(CancellationToken ct = default)
    {
        foreach (var filename in new[] { "capsule_184x69.jpg", "header.jpg", "library_hero.jpg" })
        {
            var url = $"https://cdn.akamai.steamstatic.com/steam/apps/{AppId}/{filename}";
            try
            {
                using var response = await _http.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsByteArrayAsync(ct);
            }
            catch { }
        }
        throw new InvalidOperationException("Steam 캡슐 이미지를 다운로드할 수 없습니다.");
    }

    public void Dispose() => _http.Dispose();
}

public record SteamGameData(
    string Name,
    string ShortDescription,  // appdetails.short_description
    string Price,             // e.g. "₩ 3,330"
    string OriginalPrice,     // e.g. "₩ 14,500"
    int    DiscountPct,       // e.g. 77
    int    ReviewPct,         // e.g. 86
    string ReviewLabel,       // e.g. "Very Positive"
    int    TotalReviews       // e.g. 1477
);
