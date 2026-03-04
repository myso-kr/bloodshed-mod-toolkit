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
        // Steam API는 Accept-Language 헤더가 없으면 가끔 403 반환
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 BloodshedModToolkit-SteamCard/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>가격, 할인율, 리뷰 점수를 병렬로 조회합니다.</summary>
    public async Task<SteamGameData> FetchAsync(CancellationToken ct = default)
    {
        var detailsTask = FetchAppDetailsAsync(ct);
        var reviewsTask = FetchReviewSummaryAsync(ct);
        await Task.WhenAll(detailsTask, reviewsTask);

        var (name, price, originalPrice, discountPct) = detailsTask.Result;
        var (positive, total)                          = reviewsTask.Result;

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

        return new SteamGameData(name, price, originalPrice, discountPct,
                                 reviewPct, reviewLabel, total);
    }

    // ── appdetails ────────────────────────────────────────────────────────────
    private async Task<(string name, string price, string originalPrice, int discount)>
        FetchAppDetailsAsync(CancellationToken ct)
    {
        // cc=kr: 로컬 IP 기반 자동 감지에 의존하지 않고 cc를 명시하지 않으면
        // Steam이 IP 기반으로 통화를 자동 선택합니다.
        var url = $"https://store.steampowered.com/api/appdetails?appids={AppId}";
        var json = await GetJsonAsync(url, ct);

        if (json is null) return ("Bloodshed", "N/A", "N/A", 0);

        var app = json[$"{AppId}"]?.AsObject();
        if (app is null || !(app["success"]?.GetValue<bool>() ?? false))
            return ("Bloodshed", "N/A", "N/A", 0);

        var data = app["data"]?.AsObject();
        if (data is null) return ("Bloodshed", "N/A", "N/A", 0);

        string name = data["name"]?.GetValue<string>() ?? "Bloodshed";

        var po = data["price_overview"]?.AsObject();
        if (po is null) return (name, "Free", "Free", 0);

        string finalFmt   = po["final_formatted"]?.GetValue<string>()   ?? "N/A";
        string initialFmt = po["initial_formatted"]?.GetValue<string>()  ?? finalFmt;
        int    discount   = po["discount_percent"]?.GetValue<int>()      ?? 0;

        return (name, finalFmt, initialFmt, discount);
    }

    // ── appreviews ────────────────────────────────────────────────────────────
    private async Task<(int positive, int total)> FetchReviewSummaryAsync(CancellationToken ct)
    {
        // num_per_page=0 일부 환경에서 404 → 1로 고정
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

    /// <summary>library_hero.jpg (3840×1240) 다운로드. 없으면 header.jpg 폴백.</summary>
    public async Task<byte[]> DownloadHeroAsync(CancellationToken ct = default)
    {
        foreach (var filename in new[] { "library_hero.jpg", "header.jpg" })
        {
            var url = $"https://cdn.akamai.steamstatic.com/steam/apps/{AppId}/{filename}";
            using var response = await _http.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync(ct);
        }
        throw new InvalidOperationException("Steam hero 이미지를 다운로드할 수 없습니다.");
    }

    /// <summary>logo.png 다운로드. 없으면 null 반환 (선택적 레이어).</summary>
    public async Task<byte[]?> DownloadLogoAsync(CancellationToken ct = default)
    {
        var url = $"https://cdn.akamai.steamstatic.com/steam/apps/{AppId}/logo.png";
        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // 이전 이름 호환 (Program.cs 에서 사용 중)
    public Task<byte[]> DownloadCapsuleAsync(CancellationToken ct = default)
        => DownloadHeroAsync(ct);

    public void Dispose() => _http.Dispose();
}

public record SteamGameData(
    string Name,
    string Price,           // e.g. "$3.29"
    string OriginalPrice,   // e.g. "$14.29"
    int    DiscountPct,     // e.g. 77
    int    ReviewPct,       // e.g. 85
    string ReviewLabel,     // e.g. "Very Positive"
    int    TotalReviews     // e.g. 1234
);
