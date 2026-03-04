// SteamCard — Bloodshed Steam 위젯 카드 생성기
// 사용법:
//   dotnet run --project SteamCard [출력경로] [--force]
//
//   출력경로: 기본값 = <repo-root>/docs/images/steam_card.png
//   --force : 기존 파일이 있어도 강제 재생성 (기본: 캐시 24시간)

using System.Diagnostics;
using SteamCard;

// ── 인수 파싱 ─────────────────────────────────────────────────────────────────
bool force = args.Contains("--force");
string? explicitPath = args.FirstOrDefault(a => !a.StartsWith("--"));

string repoRoot   = ResolveRepoRoot();
string outputPath = explicitPath
    ?? Path.Combine(repoRoot, "docs", "images", "steam_card.png");

// ── 캐시 검사 (--force 없으면 24시간 캐시) ───────────────────────────────────
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

// ── Steam API 조회 + 캡슐 이미지 병렬 다운로드 ─────────────────────────────
using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(20));
using var client = new SteamApiClient();

SteamGameData data;
byte[]        capsule;

try
{
    var sw = Stopwatch.StartNew();

    var dataTask    = client.FetchAsync(cts.Token);
    var capsuleTask = client.DownloadCapsuleAsync(cts.Token);
    await Task.WhenAll(dataTask, capsuleTask);

    data    = dataTask.Result;
    capsule = capsuleTask.Result;

    string priceInfo = data.DiscountPct > 0
        ? $"{data.Price} (was {data.OriginalPrice}, -{data.DiscountPct}%)"
        : data.Price;

    Console.WriteLine($"[SteamCard] {data.Name}  |  {priceInfo}  |  {data.ReviewPct}% ({data.ReviewLabel})  [{sw.ElapsedMilliseconds}ms]");
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("[SteamCard] ⚠ Steam API 타임아웃 — 카드 생성을 건너뜁니다.");
    return;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[SteamCard] ⚠ Steam API 오류: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine("[SteamCard]   네트워크 연결을 확인하세요. 카드 생성 생략.");
    return;
}

// ── 카드 렌더링 ───────────────────────────────────────────────────────────────
try
{
    await CardRenderer.RenderAsync(data, capsule, outputPath, cts.Token);
    Console.WriteLine($"[SteamCard] 저장 완료: {outputPath}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[SteamCard] ⚠ 렌더링 실패: {ex.Message}");
}

// ── 리포 루트 탐지 ────────────────────────────────────────────────────────────
static string ResolveRepoRoot()
{
    // 전략 1: CWD에 .csproj 있으면 그게 루트
    if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BloodshedModToolkit.csproj")))
        return Directory.GetCurrentDirectory();

    // 전략 2: 실행 파일 위치에서 위로 탐색
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "BloodshedModToolkit.csproj")))
            return dir.FullName;
        dir = dir.Parent;
    }

    // 전략 3: git rev-parse
    try
    {
        var psi = new ProcessStartInfo("git", "rev-parse --show-toplevel")
        {
            RedirectStandardOutput = true,
            UseShellExecute  = false,
            CreateNoWindow   = true
        };
        using var proc = Process.Start(psi)!;
        string? root = proc.StandardOutput.ReadLine()?.Trim();
        proc.WaitForExit();
        if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            return root;
    }
    catch { /* git 미설치 환경 */ }

    return Directory.GetCurrentDirectory();
}
