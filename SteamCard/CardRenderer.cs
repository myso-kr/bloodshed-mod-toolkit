using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SteamCard;

/// <summary>
/// Steam 위젯 스타일의 640×190 카드 PNG 를 렌더링합니다.
///
/// 레이아웃:
///   [library_hero.jpg 전체 배경] + [우측 그라디언트 오버레이 위에 텍스트]
///   [하단 푸터 바]
/// </summary>
public static class CardRenderer
{
    private const int W       = 640;
    private const int H       = 190;
    private const int FooterH = 28;

    // Steam 다크 팔레트
    private static readonly Color AccentBlue   = Color.ParseHex("#66c0f4");
    private static readonly Color DiscountBg   = Color.ParseHex("#4c6b22");
    private static readonly Color DiscountFg   = Color.ParseHex("#a4d007");
    private static readonly Color TextPrimary  = Color.ParseHex("#ffffff");
    private static readonly Color TextSub      = Color.ParseHex("#c6d4df");
    private static readonly Color TextDim      = Color.ParseHex("#8f98a0");
    private static readonly Color ReviewPos    = Color.ParseHex("#66c0f4");
    private static readonly Color ReviewMix    = Color.ParseHex("#b9a074");
    private static readonly Color ReviewNeg    = Color.ParseHex("#c24d2c");
    private static readonly Color Separator    = Color.ParseHex("#2a475e");
    private static readonly Color BgFooter     = Color.ParseHex("#131a21cc"); // 반투명

    public static async Task RenderAsync(
        SteamGameData data,
        byte[]        imageBytes,
        string        outputPath,
        CancellationToken ct = default)
    {
        FontFamily ff = ResolveFont();
        using var card = new Image<Rgba32>(W, H);

        card.Mutate(ctx =>
        {
            // ── 1. 배경: library_hero.jpg 를 640×190 에 꽉 채워 크롭 ─────────
            using var bg = Image.Load<Rgba32>(new MemoryStream(imageBytes));

            // 비율 유지 스케일: 짧은 쪽이 카드를 가득 채우도록
            float scaleW = (float)W / bg.Width;
            float scaleH = (float)H / bg.Height;
            float scale  = Math.Max(scaleW, scaleH);

            int sw = (int)Math.Ceiling(bg.Width  * scale);
            int sh = (int)Math.Ceiling(bg.Height * scale);
            bg.Mutate(b => b.Resize(sw, sh));

            // 중앙 크롭
            int cropX = (sw - W) / 2;
            int cropY = (sh - H) / 2;
            if (sw > W || sh > H)
                bg.Mutate(b => b.Crop(new Rectangle(cropX, cropY, W, H)));

            ctx.DrawImage(bg, new Point(0, 0), opacity: 1f);

            // ── 2. 우측 그라디언트 오버레이 (텍스트 가독성 확보) ─────────────
            // 왼쪽 투명 → 오른쪽 불투명(#1b2838) 그라디언트
            // ImageSharp는 선형 그라디언트 브러시를 지원합니다
            int gradStart = W / 3;    // x=213 부터 시작
            var gradBrush = new LinearGradientBrush(
                new PointF(gradStart, 0),
                new PointF(W, 0),
                GradientRepetitionMode.None,
                new ColorStop(0f, Color.FromRgba(0x1b, 0x28, 0x38, 0)),
                new ColorStop(0.4f, Color.FromRgba(0x1b, 0x28, 0x38, 180)),
                new ColorStop(1f, Color.FromRgba(0x1b, 0x28, 0x38, 240))
            );
            ctx.Fill(gradBrush, new RectangleF(gradStart, 0, W - gradStart, H - FooterH));

            // 전체 하단 어두운 그라디언트 (푸터 영역)
            int footerY = H - FooterH;
            ctx.Fill(Color.FromRgba(0x13, 0x1a, 0x21, 220),
                     new RectangleF(0, footerY, W, FooterH));
            ctx.Fill(Separator, new RectangleF(0, footerY, W, 1));

            // ── 3. 텍스트 영역 ────────────────────────────────────────────────
            float tx = W * 0.42f;   // 텍스트 시작 x (오른쪽 58% 영역)
            float pad = 10f;

            // 게임명
            DrawText(ctx, ff, 18, bold: true, data.Name, TextPrimary, tx, 14f);

            // 구분선
            ctx.Fill(Separator, new RectangleF(tx, 40f, W - tx - pad, 1));

            // 가격 행
            float priceY = 50f;
            RenderPriceRow(ctx, ff, data, tx, priceY);

            // 리뷰 행
            float reviewY = priceY + 32f;
            Color rColor = data.ReviewPct >= 70 ? ReviewPos
                         : data.ReviewPct >= 40 ? ReviewMix
                         :                        ReviewNeg;
            string star = data.ReviewPct >= 70 ? "★" : data.ReviewPct >= 40 ? "◆" : "✖";
            DrawText(ctx, ff, 13, bold: true,
                     $"{star}  {data.ReviewPct}% Positive", rColor, tx, reviewY);
            DrawText(ctx, ff, 10, false,
                     $"{data.ReviewLabel}  ({data.TotalReviews:N0} reviews)",
                     TextDim, tx, reviewY + 17f);

            // ── 4. 푸터 텍스트 ───────────────────────────────────────────────
            DrawText(ctx, ff, 9, false,
                     "store.steampowered.com  ·  Prices vary by region  ·  Reviews from Steam",
                     TextDim,
                     W / 2f, H - FooterH + 8f,
                     center: true);
        });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await card.SaveAsPngAsync(outputPath, ct);
    }

    private static void RenderPriceRow(
        IImageProcessingContext ctx, FontFamily ff,
        SteamGameData data, float tx, float y)
    {
        if (data.DiscountPct > 0)
        {
            // [-XX%] 배지
            string badge    = $" -{data.DiscountPct}% ";
            var    bFont    = ff.CreateFont(11, FontStyle.Bold);
            var    bSize    = TextMeasurer.MeasureSize(badge, new TextOptions(bFont));
            float  bw       = bSize.Width + 4;
            float  bh       = bSize.Height + 2;

            ctx.Fill(DiscountBg, new RectangleF(tx, y, bw, bh));
            DrawText(ctx, ff, 11, bold: true, badge, DiscountFg, tx + 2, y + 1);

            // 취소선 원가
            float sx    = tx + bw + 6f;
            var   sFont = ff.CreateFont(11, FontStyle.Regular);
            var   sSize = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(sFont));
            DrawText(ctx, ff, 11, false, data.OriginalPrice, TextDim, sx, y + 2);
            ctx.Fill(TextDim,
                     new RectangleF(sx, y + sSize.Height / 2f + 2, sSize.Width, 1));

            // 할인가
            DrawText(ctx, ff, 15, bold: true, data.Price,
                     AccentBlue, sx + sSize.Width + 8, y);
        }
        else
        {
            DrawText(ctx, ff, 15, bold: true, data.Price, AccentBlue, tx, y);
        }
    }

    private static FontFamily ResolveFont()
    {
        string[] candidates =
            ["Segoe UI", "Roboto", "Arial", "DejaVu Sans", "Liberation Sans"];
        foreach (var name in candidates)
            if (SystemFonts.TryGet(name, out FontFamily ff))
                return ff;
        return SystemFonts.Collection.Families.First();
    }

    private static void DrawText(
        IImageProcessingContext ctx, FontFamily ff,
        float size, bool bold, string text, Color color,
        float x, float y, bool center = false)
    {
        var font = ff.CreateFont(size, bold ? FontStyle.Bold : FontStyle.Regular);
        if (center)
        {
            var measured = TextMeasurer.MeasureSize(text, new TextOptions(font));
            x -= measured.Width / 2f;
        }
        ctx.DrawText(text, font, color, new PointF(x, y));
    }
}
