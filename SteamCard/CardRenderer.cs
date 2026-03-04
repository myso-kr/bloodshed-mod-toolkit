using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SteamCard;

/// <summary>
/// 646×190 Steam 위젯 iframe 디자인 복원 렌더러.
///
/// 공식 CSS(styles_widget.css / store.css)에서 추출한 실제 값 사용:
///   - 배경: linear-gradient(130deg, #3b4351, #282e39)
///   - 이미지: capsule_184x69 (float left)
///   - 헤더: "Buy [Name]" #fefefe + " on Steam" #9e9e9e  (21px Regular)
///   - 설명: 13px #c9c9c9, 캡슐 우측 흘러감
///   - 구매 영역(우하단): [discount_pct][discount_prices][Buy on Steam]
///       - discount_pct  : bg #4c6b22, fg #BEEE11, 25px, h=32
///       - discount_prices: bg #344654, 원가 #738895 11px / 최종가 #BEEE11 14px
///       - 버튼           : gradient #6fa720→#588a1b, text #d2efa9 14px
///   - 플랫폼 아이콘: 좌하단 Windows 4-square
/// </summary>
public static class CardRenderer
{
    // ── 캔버스 ────────────────────────────────────────────────────────────────
    private const int W = 646;
    private const int H = 190;

    // ── 레이아웃 ──────────────────────────────────────────────────────────────
    private const int PadH    = 15;   // 좌우 여백
    private const int PadV    = 10;   // 상하 여백
    private const int CapsW   = 184;  // capsule_184x69 너비
    private const int CapsH   = 69;   // capsule_184x69 높이
    private const int CapsGap = 10;   // 캡슐 우측 여백
    private const int HeaderH = 28;   // h1 line-height

    // ── 공식 Steam 위젯 색상 (styles_widget.css / store.css) ──────────────────
    private static readonly Color BgStart       = Color.ParseHex("#3b4351");
    private static readonly Color BgEnd         = Color.ParseHex("#282e39");
    private static readonly Color BorderColor   = Color.ParseHex("#424c5c");
    private static readonly Color TitleFg       = Color.ParseHex("#fefefe");
    private static readonly Color OnSteamFg     = Color.ParseHex("#9e9e9e");
    private static readonly Color DescFg        = Color.ParseHex("#c9c9c9");
    private static readonly Color DiscPctBg     = Color.ParseHex("#4c6b22");
    private static readonly Color DiscPctFg     = Color.ParseHex("#BEEE11");
    private static readonly Color DiscPriceBg   = Color.ParseHex("#344654");
    private static readonly Color OrigPriceFg   = Color.ParseHex("#738895");
    private static readonly Color FinalPriceFg  = Color.ParseHex("#BEEE11");
    private static readonly Color PurchaseBg    = Color.ParseHex("#000000");
    private static readonly Color CartBgStart   = Color.ParseHex("#6fa720");
    private static readonly Color CartBgEnd     = Color.ParseHex("#588a1b");
    private static readonly Color CartFg        = Color.ParseHex("#d2efa9");
    private static readonly Color PlatformFg    = Color.ParseHex("#a0a0a0");

    public static async Task RenderAsync(
        SteamGameData data,
        byte[]        capsuleBytes,
        string        outputPath,
        CancellationToken ct = default)
    {
        FontFamily ff = ResolveFont();
        using var card = new Image<Rgba32>(W, H);

        card.Mutate(ctx =>
        {
            // ── 1. 배경 그라디언트 (130deg ≈ 대각선) ──────────────────────────
            var bgGrad = new LinearGradientBrush(
                new PointF(0, 0), new PointF(W, H),
                GradientRepetitionMode.None,
                new ColorStop(0f, BgStart),
                new ColorStop(1f, BgEnd)
            );
            ctx.Fill(bgGrad, new RectangleF(0, 0, W, H));

            // ── 2. 테두리 (상단 + 좌측, 1px #424c5c) ─────────────────────────
            ctx.Fill(BorderColor, new RectangleF(0, 0, W, 1));
            ctx.Fill(BorderColor, new RectangleF(0, 0, 1, H));

            // ── 3. 헤더: "Buy [Name]" (white) + " on Steam" (gray) ───────────
            var titleFont    = ff.CreateFont(21, FontStyle.Regular);
            string buyText   = $"Buy {data.Name}";
            var    buySize   = TextMeasurer.MeasureSize(buyText, new TextOptions(titleFont));
            ctx.DrawText(buyText,     titleFont, TitleFg,   new PointF(PadH, PadV));
            ctx.DrawText(" on Steam", titleFont, OnSteamFg, new PointF(PadH + buySize.Width, PadV));

            // ── 4. 캡슐 이미지 (184×69, 헤더 아래) ───────────────────────────
            float capsY = PadV + HeaderH + 4;
            using var cimg = Image.Load<Rgba32>(new MemoryStream(capsuleBytes));
            // 184×69에 맞게 center-crop
            float sc = Math.Max((float)CapsW / cimg.Width, (float)CapsH / cimg.Height);
            int   sw = (int)Math.Ceiling(cimg.Width  * sc);
            int   sh = (int)Math.Ceiling(cimg.Height * sc);
            cimg.Mutate(c => c.Resize(sw, sh));
            int cx = (sw - CapsW) / 2, cy = (sh - CapsH) / 2;
            if (sw > CapsW || sh > CapsH)
                cimg.Mutate(c => c.Crop(new Rectangle(cx, cy, CapsW, CapsH)));
            ctx.DrawImage(cimg, new Point(PadH, (int)capsY), 1f);

            // ── 5. 설명 텍스트 (캡슐 우측, 13px #c9c9c9) ─────────────────────
            float descX = PadH + CapsW + CapsGap;
            float descW = W - descX - PadH;
            string desc = data.ShortDescription;
            if (desc.Length > 160) desc = desc[..157] + "...";
            var descFont = ff.CreateFont(13, FontStyle.Regular);
            ctx.DrawText(
                new RichTextOptions(descFont)
                {
                    Origin          = new PointF(descX, capsY + 2),
                    WrappingLength  = descW,
                    HorizontalAlignment = HorizontalAlignment.Left,
                },
                desc, DescFg);

            // ── 6. 플랫폼 아이콘 (좌하단, Windows 4-square) ───────────────────
            float iconY = H - PadV - 11;
            const float sq = 5f, gap = 1f;
            ctx.Fill(PlatformFg, new RectangleF(PadH,          iconY,       sq, sq));
            ctx.Fill(PlatformFg, new RectangleF(PadH + sq + gap, iconY,     sq, sq));
            ctx.Fill(PlatformFg, new RectangleF(PadH,          iconY+sq+gap, sq, sq));
            ctx.Fill(PlatformFg, new RectangleF(PadH + sq + gap, iconY+sq+gap, sq, sq));

            // ── 7. 구매 영역 (우하단, 32px 고정 높이) ─────────────────────────
            const float purchH = 32f;
            float       purchY = H - PadV - purchH;
            RenderPurchaseArea(ctx, ff, data, purchY, purchH);
        });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await card.SaveAsPngAsync(outputPath, ct);
    }

    // ── 구매 영역 ─────────────────────────────────────────────────────────────
    private static void RenderPurchaseArea(
        IImageProcessingContext ctx, FontFamily ff,
        SteamGameData data, float y, float h)
    {
        const string btnLabel = "Buy on Steam";
        var btnFont  = ff.CreateFont(14, FontStyle.Regular);
        var btnSize  = TextMeasurer.MeasureSize(btnLabel, new TextOptions(btnFont));
        float btnW   = btnSize.Width + 22;   // 11px padding × 2

        if (data.DiscountPct > 0)
        {
            // ── 할인 배지 블록 ────────────────────────────────────────────────
            string pctLabel = $"-{data.DiscountPct}%";
            var pctFont = ff.CreateFont(25, FontStyle.Bold);
            var pctSize = TextMeasurer.MeasureSize(pctLabel, new TextOptions(pctFont));
            float pctW  = pctSize.Width + 12;   // 6px padding × 2

            // ── 가격 블록 ─────────────────────────────────────────────────────
            var origFont = ff.CreateFont(11, FontStyle.Regular);
            var origSize = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(origFont));
            var finFont  = ff.CreateFont(14, FontStyle.Bold);
            var finSize  = TextMeasurer.MeasureSize(data.Price,          new TextOptions(finFont));
            float priceContentW = Math.Max(origSize.Width, finSize.Width);
            float priceW = priceContentW + 14;  // 6px left + 8px right

            float totalW = pctW + priceW + btnW;
            float x0     = W - PadH - totalW;

            // 블랙 컨테이너 (전체 purchase 영역)
            ctx.Fill(PurchaseBg, new RectangleF(x0, y, totalW, h));

            // 할인율 블록 (#4c6b22)
            ctx.Fill(DiscPctBg, new RectangleF(x0, y, pctW, h));
            float pctTY = y + (h - pctSize.Height) / 2f;
            ctx.DrawText(pctLabel, pctFont, DiscPctFg, new PointF(x0 + 6, pctTY));

            // 가격 블록 (#344654)
            float px = x0 + pctW;
            ctx.Fill(DiscPriceBg, new RectangleF(px, y, priceW, h));
            // 원가 (취소선)
            float origTY = y + 5;
            ctx.DrawText(data.OriginalPrice, origFont, OrigPriceFg, new PointF(px + 6, origTY));
            ctx.Fill(OrigPriceFg, new RectangleF(px + 6, origTY + origSize.Height / 2f + 1, origSize.Width, 1));
            // 최종가
            float finTY = y + h - finSize.Height - 3;
            ctx.DrawText(data.Price, finFont, FinalPriceFg, new PointF(px + 6, finTY));

            // Buy on Steam 버튼 (그라디언트)
            float bx = px + priceW;
            RenderBuyButton(ctx, ff, btnLabel, btnFont, bx, y, btnW, h);
        }
        else
        {
            // 할인 없음: 가격 블록 + 버튼
            var   priceFont = ff.CreateFont(14, FontStyle.Bold);
            var   priceSize = TextMeasurer.MeasureSize(data.Price, new TextOptions(priceFont));
            float priceW    = priceSize.Width + 16;
            float totalW    = priceW + btnW;
            float x0        = W - PadH - totalW;

            ctx.Fill(PurchaseBg,   new RectangleF(x0,         y, totalW, h));
            ctx.Fill(DiscPriceBg,  new RectangleF(x0,         y, priceW, h));
            ctx.DrawText(
                new RichTextOptions(priceFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Origin = new PointF(x0 + priceW / 2f, y + h / 2f),
                },
                data.Price, FinalPriceFg);

            RenderBuyButton(ctx, ff, btnLabel, btnFont, x0 + priceW, y, btnW, h);
        }
    }

    private static void RenderBuyButton(
        IImageProcessingContext ctx, FontFamily ff,
        string label, Font font,
        float x, float y, float w, float h)
    {
        var grad = new LinearGradientBrush(
            new PointF(x, y), new PointF(x + w, y),
            GradientRepetitionMode.None,
            new ColorStop(0.05f, CartBgStart),
            new ColorStop(0.95f, CartBgEnd)
        );
        ctx.Fill(grad, new RectangleF(x, y, w, h));
        ctx.DrawText(
            new RichTextOptions(font)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Origin = new PointF(x + w / 2f, y + h / 2f),
            },
            label, CartFg);
    }

    // ── 폰트 탐색 ─────────────────────────────────────────────────────────────
    private static FontFamily ResolveFont()
    {
        string[] candidates = ["Segoe UI", "Roboto", "Arial", "DejaVu Sans", "Liberation Sans"];
        foreach (var name in candidates)
            if (SystemFonts.TryGet(name, out FontFamily ff))
                return ff;
        return SystemFonts.Collection.Families.First();
    }
}
