using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SteamCard;

/// <summary>
/// 646×190 Steam 위젯 iframe 표준 크기 카드 렌더러.
///
/// 레이어 구조:
///   1) library_hero.jpg — 전체 배경 (center-crop, full bleed)
///   2) 좌→우 그라디언트 오버레이 (x=130 투명 → x=218 불투명)
///   3) logo.png — 좌측 영역 수직 중앙 배치
///   4) 우측 텍스트: 게임명 / 가격 / 리뷰 / Add to Cart 버튼
/// </summary>
public static class CardRenderer
{
    // ── 캔버스 ────────────────────────────────────────────────────────────────
    private const int W = 646;
    private const int H = 190;

    // ── 레이아웃 ──────────────────────────────────────────────────────────────
    private const int GradStartX = 128;   // 그라디언트 시작 (투명)
    private const int GradEndX   = 220;   // 그라디언트 종료 (불투명)
    private const int TextX      = 226;   // 텍스트/솔리드 시작 x
    private const int TextPad    = 12;    // 텍스트 내부 여백
    private const int LogoMaxW   = 172;   // 로고 최대 너비
    private const int LogoMaxH   = 106;   // 로고 최대 높이

    // ── 버튼 ──────────────────────────────────────────────────────────────────
    private const int BtnW = 206;
    private const int BtnH = 26;

    // ── Steam 다크 팔레트 ─────────────────────────────────────────────────────
    private static readonly Color BgOverlay  = Color.ParseHex("#1b2838");
    private static readonly Color Separator  = Color.ParseHex("#2a475e");
    private static readonly Color AccentBlue = Color.ParseHex("#66c0f4");
    private static readonly Color DiscountBg = Color.ParseHex("#4c6b22");
    private static readonly Color DiscountFg = Color.ParseHex("#a4d007");
    private static readonly Color TextWhite  = Color.ParseHex("#ffffff");
    private static readonly Color TextDim    = Color.ParseHex("#7a8b96");
    private static readonly Color ReviewPos  = Color.ParseHex("#57cbde");
    private static readonly Color ReviewMix  = Color.ParseHex("#c9b27c");
    private static readonly Color ReviewNeg  = Color.ParseHex("#c24d2c");
    private static readonly Color CartBg     = Color.ParseHex("#4d7a16");
    private static readonly Color CartTop    = Color.ParseHex("#71c102");  // 상단 하이라이트

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
            // ── 1. 배경: library_hero.jpg center-crop ─────────────────────────
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
            var grad = new LinearGradientBrush(
                new PointF(GradStartX, 0), new PointF(GradEndX, 0),
                GradientRepetitionMode.None,
                new ColorStop(0f,   Color.FromRgba(0x1b, 0x28, 0x38, 0)),
                new ColorStop(0.6f, Color.FromRgba(0x1b, 0x28, 0x38, 195)),
                new ColorStop(1f,   Color.FromRgba(0x1b, 0x28, 0x38, 250))
            );
            ctx.Fill(grad, new RectangleF(GradStartX, 0, W - GradStartX, H));
            ctx.Fill(BgOverlay, new RectangleF(TextX, 0, W - TextX, H));

            // ── 3. 게임 로고 (좌측 영역 수직 중앙) ───────────────────────────
            if (logoBytes is not null)
            {
                using var logo = Image.Load<Rgba32>(new MemoryStream(logoBytes));
                float logoScale = Math.Min(
                    (float)LogoMaxW / logo.Width,
                    (float)LogoMaxH / logo.Height);
                if (logoScale < 1f)
                    logo.Mutate(l => l.Resize(
                        (int)(logo.Width  * logoScale),
                        (int)(logo.Height * logoScale)));

                // 좌측 존(0..GradEndX) 수직 중앙
                int logoX = (GradEndX - logo.Width) / 2;
                int logoY = (H - logo.Height) / 2;
                ctx.DrawImage(logo, new Point(Math.Max(0, logoX), Math.Max(0, logoY)), 1f);
            }

            // ── 4. 우측 텍스트 영역 ───────────────────────────────────────────
            float tx = TextX + TextPad;
            float tw = W - tx - TextPad;

            // 게임명
            var titleFont = ff.CreateFont(16, FontStyle.Bold);
            ctx.DrawText(data.Name, titleFont, TextWhite, new PointF(tx, 13));

            // 구분선 1
            ctx.Fill(Separator, new RectangleF(tx, 39, tw, 1));

            // 가격 행
            RenderPriceRow(ctx, ff, data, tx, 46);

            // 구분선 2
            ctx.Fill(Separator, new RectangleF(tx, 82, tw, 1));

            // 리뷰 행
            RenderReviewRow(ctx, ff, data, tx, 89);

            // Add to Cart 버튼 (하단 고정)
            RenderCartButton(ctx, ff, tx, H - BtnH - 10);
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
            // [-XX%] 배지 — 폰트 사이즈 기반 고정 높이로 배경 잘림 방지
            string badge    = $"-{data.DiscountPct}%";
            var    badgeFont = ff.CreateFont(11, FontStyle.Bold);
            var    bSize     = TextMeasurer.MeasureSize(badge, new TextOptions(badgeFont));
            float  bh        = (float)Math.Ceiling(badgeFont.Size * 2.2f);   // 고정 높이
            float  bw        = bSize.Width + 16;
            float  textOffY  = (bh - bSize.Height) / 2f;

            ctx.Fill(DiscountBg, new RectangleF(tx, y, bw, bh));
            ctx.DrawText(badge, badgeFont, DiscountFg, new PointF(tx + 8, y + textOffY));

            // 취소선 원가
            float   sx       = tx + bw + 8;
            var     origFont = ff.CreateFont(11, FontStyle.Regular);
            var     oSize    = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(origFont));
            float   origOffY = (bh - oSize.Height) / 2f;
            ctx.DrawText(data.OriginalPrice, origFont, TextDim, new PointF(sx, y + origOffY));
            ctx.Fill(TextDim, new RectangleF(sx, y + bh / 2f, oSize.Width, 1));

            // 할인가 (크게)
            var priceFont = ff.CreateFont(18, FontStyle.Bold);
            var pSize     = TextMeasurer.MeasureSize(data.Price, new TextOptions(priceFont));
            float priceOffY = (bh - pSize.Height) / 2f;
            ctx.DrawText(data.Price, priceFont, AccentBlue,
                         new PointF(sx + oSize.Width + 10, y + priceOffY));
        }
        else
        {
            var priceFont = ff.CreateFont(18, FontStyle.Bold);
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

        const float dotSize = 9f;
        ctx.Fill(rColor, new RectangleF(tx, y + 5, dotSize, dotSize));

        var pctFont = ff.CreateFont(13, FontStyle.Bold);
        ctx.DrawText($"{data.ReviewPct}% Positive", pctFont, rColor,
                     new PointF(tx + dotSize + 7, y));

        var subFont = ff.CreateFont(10, FontStyle.Regular);
        ctx.DrawText(
            $"{data.ReviewLabel}   ·   {data.TotalReviews:N0} reviews",
            subFont, TextDim, new PointF(tx + dotSize + 7, y + 19));
    }

    // ── Add to Cart 버튼 ──────────────────────────────────────────────────────
    private static void RenderCartButton(
        IImageProcessingContext ctx, FontFamily ff,
        float tx, float y)
    {
        // 버튼 배경
        ctx.Fill(CartBg, new RectangleF(tx, y, BtnW, BtnH));
        // 상단 1px 하이라이트 (Steam 버튼 특유의 그라디언트 느낌)
        ctx.Fill(CartTop, new RectangleF(tx, y, BtnW, 2));

        // 중앙 정렬 텍스트
        var btnFont = ff.CreateFont(11, FontStyle.Bold);
        ctx.DrawText(
            new RichTextOptions(btnFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Origin = new PointF(tx + BtnW / 2f, y + BtnH / 2f + 1),
            },
            "Add to Cart", TextWhite);
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
