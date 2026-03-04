// SteamCard — Bloodshed Steam 위젯 카드 생성기
// 사용법:
//   dotnet run --project SteamCard [출력경로] [--force]
//
//   출력경로: 기본값 = <repo-root>/docs/images/steam_card.png
//   --force : 기존 파일이 있어도 강제 재생성 (기본: 캐시 24시간)

using System.Diagnostics;
using SteamCard;

bool force = args.Contains("--force");
string? explicitPath = args.FirstOrDefault(a => !a.StartsWith("--"));

string repoRoot   = ResolveRepoRoot();
string outputPath = explicitPath
    ?? Path.Combine(repoRoot, "docs", "images", "steam_card.png");

if (!force && File.Exists(outputPath))
{
    var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(outputPath);
    if (age.TotalHours < 24)
    {
        Console.WriteLine($"[SteamCard] 캐시 유효 ({age.TotalMinutes:F0}분 경과) — 재생성 생략.");
        Console.WriteLine($"[SteamCard] 강제 재생성: --force 옵션 사용");
        return;
    }
}

Console.WriteLine($"[SteamCard] 출력: {outputPath}");

using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(25));
using var client = new SteamApiClient();

SteamGameData data;
byte[]        hero;
byte[]?       logo;

try
{
    var sw = Stopwatch.StartNew();

    var dataTask = client.FetchAsync(cts.Token);
    var heroTask = client.DownloadHeroAsync(cts.Token);
    var logoTask = client.DownloadLogoAsync(cts.Token);
    await Task.WhenAll(dataTask, heroTask, logoTask);

    data = dataTask.Result;
    hero = heroTask.Result;
    logo = logoTask.Result;

    string priceInfo = data.DiscountPct > 0
        ? $"{data.Price} (was {data.OriginalPrice}, -{data.DiscountPct}%)"
        : data.Price;

    Console.WriteLine(
        $"[SteamCard] {data.Name}  |  {priceInfo}  |  {data.ReviewPct}% ({data.ReviewLabel})" +
        $"  |  logo={logo is not null}  [{sw.ElapsedMilliseconds}ms]");
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("[SteamCard] ⚠ 타임아웃 — 카드 생성 생략.");
    return;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[SteamCard] ⚠ 오류: {ex.GetType().Name}: {ex.Message}");
    return;
}

try
{
    await CardRenderer.RenderAsync(data, hero, logo, outputPath, cts.Token);
    Console.WriteLine($"[SteamCard] 저장 완료: {outputPath}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[SteamCard] ⚠ 렌더링 실패: {ex.Message}");
}

static string ResolveRepoRoot()
{
    if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BloodshedModToolkit.csproj")))
        return Directory.GetCurrentDirectory();

    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "BloodshedModToolkit.csproj")))
            return dir.FullName;
        dir = dir.Parent;
    }

    try
    {
        var psi = new ProcessStartInfo("git", "rev-parse --show-toplevel")
        {
            RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        string? root = proc.StandardOutput.ReadLine()?.Trim();
        proc.WaitForExit();
        if (!string.IsNullOrEmpty(root) && Directory.Exists(root)) return root;
    }
    catch { }

    return Directory.GetCurrentDirectory();
}
