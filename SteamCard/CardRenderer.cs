using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SteamCard;

/// <summary>
/// 1260×540 Steam 라이브러리 카드 렌더러.
///
/// 레이어 구조:
///   1) library_hero.jpg — 전체 배경 (center-crop, full bleed)
///   2) 좌→우 그라디언트 오버레이 (x=380 투명 → x=680 불투명)
///   3) logo.png — 좌측 영역 중앙 배치 (있을 때)
///   4) 우측 텍스트: 게임명 / 가격 / 리뷰
///   5) 하단 푸터 바
/// </summary>
public static class CardRenderer
{
    // ── 캔버스 ────────────────────────────────────────────────────────────────
    private const int W       = 1260;
    private const int H       = 540;
    private const int FooterH = 52;

    // ── 레이아웃 ──────────────────────────────────────────────────────────────
    private const int GradStartX = 380;   // 그라디언트 시작 (투명)
    private const int GradEndX   = 700;   // 그라디언트 종료 (불투명)
    private const int TextX      = 720;   // 텍스트 시작 x
    private const int TextPad    = 28;    // 텍스트 내부 여백
    private const int LogoMaxW   = 440;   // 로고 최대 너비
    private const int LogoMaxH   = 220;   // 로고 최대 높이

    // ── Steam 다크 팔레트 ─────────────────────────────────────────────────────
    private static readonly Color BgOverlay   = Color.ParseHex("#1b2838");
    private static readonly Color BgFooter    = Color.ParseHex("#0e1620");
    private static readonly Color Separator   = Color.ParseHex("#2a475e");
    private static readonly Color AccentBlue  = Color.ParseHex("#66c0f4");
    private static readonly Color DiscountBg  = Color.ParseHex("#4c6b22");
    private static readonly Color DiscountFg  = Color.ParseHex("#a4d007");
    private static readonly Color TextWhite   = Color.ParseHex("#ffffff");
    private static readonly Color TextSub     = Color.ParseHex("#c6d4df");
    private static readonly Color TextDim     = Color.ParseHex("#7a8b96");
    private static readonly Color ReviewPos   = Color.ParseHex("#57cbde");  // Steam 파랑
    private static readonly Color ReviewMix   = Color.ParseHex("#c9b27c");
    private static readonly Color ReviewNeg   = Color.ParseHex("#c24d2c");

    public static async Task RenderAsync(
        SteamGameData data,
        byte[]        heroBytes,
        byte[]?       logoBytes,
        string        outputPath,
        CancellationToken ct = default)
    {
        FontFamily ff = ResolveFont();
        using var card = new Image<Rgba32>(W, H);

        card.Mutate(ctx =>
        {
            // ── 1. 배경: library_hero.jpg center-crop → 전체 채우기 ─────────
            using var bg = Image.Load<Rgba32>(new MemoryStream(heroBytes));
            float scale  = Math.Max((float)W / bg.Width, (float)H / bg.Height);
            int   sw     = (int)Math.Ceiling(bg.Width  * scale);
            int   sh     = (int)Math.Ceiling(bg.Height * scale);
            bg.Mutate(b => b.Resize(sw, sh));
            int cropX = (sw - W) / 2;
            int cropY = (sh - H) / 2;
            if (sw > W || sh > H)
                bg.Mutate(b => b.Crop(new Rectangle(cropX, cropY, W, H)));
            ctx.DrawImage(bg, Point.Empty, 1f);

            // ── 2. 좌→우 그라디언트 오버레이 ─────────────────────────────────
            int contentH = H - FooterH;
            var grad = new LinearGradientBrush(
                new PointF(GradStartX, 0), new PointF(GradEndX, 0),
                GradientRepetitionMode.None,
                new ColorStop(0f,   Color.FromRgba(0x1b, 0x28, 0x38, 0)),
                new ColorStop(0.6f, Color.FromRgba(0x1b, 0x28, 0x38, 195)),
                new ColorStop(1f,   Color.FromRgba(0x1b, 0x28, 0x38, 248))
            );
            ctx.Fill(grad, new RectangleF(GradStartX, 0, W - GradStartX, contentH));
            // TextX 이후는 완전 불투명
            ctx.Fill(BgOverlay, new RectangleF(TextX, 0, W - TextX, contentH));

            // ── 3. 푸터 바 ────────────────────────────────────────────────────
            ctx.Fill(BgFooter, new RectangleF(0, contentH, W, FooterH));
            ctx.Fill(Separator, new RectangleF(0, contentH, W, 1));

            // ── 4. 게임 로고 (좌측 영역) ──────────────────────────────────────
            if (logoBytes is not null)
            {
                using var logo = Image.Load<Rgba32>(new MemoryStream(logoBytes));
                // 비율 유지 축소: LogoMaxW × LogoMaxH 안에 맞추기
                float logoScale = Math.Min(
                    (float)LogoMaxW / logo.Width,
                    (float)LogoMaxH / logo.Height);
                if (logoScale < 1f)
                    logo.Mutate(l => l.Resize(
                        (int)(logo.Width  * logoScale),
                        (int)(logo.Height * logoScale)));

                // 좌측 절반(0..GradEndX) 의 중앙 하단 정렬
                int logoX = (GradEndX - logo.Width) / 2;
                int logoY = contentH - logo.Height - 40;
                ctx.DrawImage(logo, new Point(Math.Max(0, logoX), Math.Max(0, logoY)), 1f);
            }

            // ── 5. 우측 텍스트 영역 ───────────────────────────────────────────
            float tx = TextX + TextPad;
            float tw = W - tx - TextPad;    // 텍스트 최대 너비

            // 게임명
            var titleFont = ff.CreateFont(46, FontStyle.Bold);
            ctx.DrawText(data.Name, titleFont, TextWhite, new PointF(tx, 60));

            // 구분선
            float ruleY = 124f;
            ctx.Fill(Separator, new RectangleF(tx, ruleY, tw, 2));

            // 가격 행
            float priceY = ruleY + 24;
            RenderPriceRow(ctx, ff, data, tx, priceY);

            // 구분선 2
            float ruleY2 = priceY + 80;
            ctx.Fill(Separator, new RectangleF(tx, ruleY2, tw, 1));

            // 리뷰 행
            float reviewY = ruleY2 + 22;
            RenderReviewRow(ctx, ff, data, tx, reviewY);

            // ── 6. 푸터 텍스트 ────────────────────────────────────────────────
            var footerFont = ff.CreateFont(18, FontStyle.Regular);
            string footerText =
                "store.steampowered.com   ·   Prices vary by region   ·   Reviews sourced from Steam";
            ctx.DrawText(
                new RichTextOptions(footerFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Origin = new PointF(W / 2f, contentH + FooterH / 2f),
                },
                footerText, TextDim);
        });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await card.SaveAsPngAsync(outputPath, ct);
    }

    // ── 가격 행 ───────────────────────────────────────────────────────────────
    private static void RenderPriceRow(
        IImageProcessingContext ctx, FontFamily ff,
        SteamGameData data, float tx, float y)
    {
        if (data.DiscountPct > 0)
        {
            // [-XX%] 배지
            string badge  = $"  -{data.DiscountPct}%  ";
            var badgeFont = ff.CreateFont(22, FontStyle.Bold);
            var bSize     = TextMeasurer.MeasureSize(badge, new TextOptions(badgeFont));
            float bh      = bSize.Height + 6;
            float bw      = bSize.Width  + 4;

            ctx.Fill(DiscountBg, new RectangleF(tx, y, bw, bh));
            ctx.DrawText(badge, badgeFont, DiscountFg, new PointF(tx + 2, y + 3));

            // 취소선 원가
            float sx    = tx + bw + 14;
            var origFont = ff.CreateFont(20, FontStyle.Regular);
            var oSize    = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(origFont));
            ctx.DrawText(data.OriginalPrice, origFont, TextDim, new PointF(sx, y + 4));
            // 취소선
            ctx.Fill(TextDim, new RectangleF(sx, y + oSize.Height / 2f + 5, oSize.Width, 2));

            // 할인가 (크게)
            var priceFont = ff.CreateFont(36, FontStyle.Bold);
            ctx.DrawText(data.Price, priceFont, AccentBlue,
                         new PointF(sx + oSize.Width + 16, y - 3));
        }
        else
        {
            var priceFont = ff.CreateFont(36, FontStyle.Bold);
            ctx.DrawText(data.Price, priceFont, AccentBlue, new PointF(tx, y));
        }
    }

    // ── 리뷰 행 ───────────────────────────────────────────────────────────────
    private static void RenderReviewRow(
        IImageProcessingContext ctx, FontFamily ff,
        SteamGameData data, float tx, float y)
    {
        Color rColor = data.ReviewPct >= 70 ? ReviewPos
                     : data.ReviewPct >= 40 ? ReviewMix
                     :                        ReviewNeg;

        // 컬러 인디케이터 사각형 (이모지 대체)
        const float dotSize = 14f;
        ctx.Fill(rColor, new RectangleF(tx, y + 9, dotSize, dotSize));

        // "XX% Positive"
        var pctFont = ff.CreateFont(28, FontStyle.Bold);
        ctx.DrawText($"{data.ReviewPct}% Positive", pctFont, rColor,
                     new PointF(tx + dotSize + 12, y));

        // "ReviewLabel · N reviews"
        var subFont = ff.CreateFont(20, FontStyle.Regular);
        ctx.DrawText(
            $"{data.ReviewLabel}   ·   {data.TotalReviews:N0} reviews",
            subFont, TextDim, new PointF(tx + dotSize + 12, y + 38));
    }

    // ── 폰트 탐색 ─────────────────────────────────────────────────────────────
    private static FontFamily ResolveFont()
    {
        string[] candidates =
            ["Segoe UI", "Roboto", "Arial", "DejaVu Sans", "Liberation Sans"];
        foreach (var name in candidates)
            if (SystemFonts.TryGet(name, out FontFamily ff))
                return ff;
        return SystemFonts.Collection.Families.First();
    }
}
