using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SteamCard;

/// <summary>
/// 646×190 Steam 위젯 iframe 픽셀 정확도 복원 렌더러.
///
/// 측정값 출처: styles_widget.css + store.css
///
/// 위젯 레이아웃:
///   #widget: height=146px (content-box) + padding=10px 15px → DOM total=166px
///   .header_container h1: line-height=28px  → y=10..38
///   .desc: margin=10px 10px 0 0           → y=48..
///   .capsule (float:left): margin=2px 10px 10px 0 → x=15, y=50, w=184, h=69
///   .desc text:                            → x=209, y=48, maxW=412
///   .game_area_purchase_platform: absolute, bottom=13px left=15px → y_bot=153
///   .game_purchase_action: absolute, bottom=-20px, right=10px
///     → container y=150..186, h=36 (32 content + 4 padding)
///     → blocks (pct/prices/btn) y=152, h=32
/// </summary>
public static class CardRenderer
{
    // ── 캔버스 ────────────────────────────────────────────────────────────────
    private const int W = 646;
    private const int H = 190;   // iframe 전체 높이

    // ── 위젯 레이아웃 (styles_widget.css 정확 수치) ───────────────────────────
    // #widget: height=146px content-box, padding=10px 15px → DOM h=166
    private const float WidgetH     = 166f;  // 146 + 10×2
    private const float PadH        = 15f;   // 좌우 패딩
    private const float PadV        = 10f;   // 상하 패딩
    private const float HeaderH     = 28f;   // h1 line-height
    private const float CapsW       = 184f;
    private const float CapsH       = 69f;
    private const float CapsMargTop = 2f;    // capsule margin-top (within .desc)
    private const float CapsMargRt  = 10f;   // capsule margin-right

    // 파생 위치
    // .desc: margin-top=10px → y = PadV + HeaderH + 10 = 48
    private const float DescY    = PadV + HeaderH + 10f;        // 48
    private const float CapsX    = PadH;                        // 15
    private const float CapsY    = DescY + CapsMargTop;         // 50
    private const float TextX    = CapsX + CapsW + CapsMargRt;  // 209
    // .desc margin-right=10px → desc ends at W-PadH-10=621 → textW=621-209=412
    private const float TextW    = W - PadH - 10f - TextX;      // 412

    // 플랫폼 아이콘: position=absolute, bottom=13px, left=15px
    // y_bottom = WidgetH - 13 = 153
    private const float PlatIconBotY = WidgetH - 13f;           // 153

    // 구매 영역: .game_purchase_action bottom=-20px, right=10px
    // → element bottom = WidgetH + 20 = 186
    // → .game_purchase_action_bg: height=32 content + padding=2px 2px 2px 0 → total=36px
    // → container top = 186 - 36 = 150
    // → blocks inside (after top padding 2px): y=152, h=32
    // → right edge of container = W - 10 = 636
    // → right edge of content (after right padding 2px) = 634
    private const float PurchContY      = 150f;
    private const float PurchContH      = 36f;
    private const float PurchBlockY     = 152f;
    private const float PurchBlockH     = 32f;
    private const float PurchContRight  = 636f;  // W - 10
    private const float PurchBlockRight = 634f;  // 636 - 2 (right pad)

    // ── 정확한 색상 (styles_widget.css + store.css) ───────────────────────────
    private static readonly Color BgStart      = Color.ParseHex("#3b4351");
    private static readonly Color BgEnd        = Color.ParseHex("#282e39");
    private static readonly Color BorderClr    = Color.ParseHex("#424c5c");
    private static readonly Color TitleClr     = Color.ParseHex("#fefefe");
    private static readonly Color OnSteamClr   = Color.ParseHex("#9e9e9e");
    private static readonly Color DescClr      = Color.ParseHex("#c9c9c9");
    private static readonly Color DiscPctBg    = Color.ParseHex("#4c6b22");
    private static readonly Color DiscPctFg    = Color.ParseHex("#BEEE11");
    private static readonly Color DiscPriceBg  = Color.ParseHex("#344654");
    private static readonly Color OrigPriceClr = Color.ParseHex("#738895");
    private static readonly Color FinPriceClr  = Color.ParseHex("#BEEE11");
    private static readonly Color PurchBg      = Color.ParseHex("#000000");
    private static readonly Color CartBgL      = Color.ParseHex("#6fa720");
    private static readonly Color CartBgR      = Color.ParseHex("#588a1b");
    private static readonly Color CartFg       = Color.ParseHex("#d2efa9");
    private static readonly Color PlatClr      = Color.ParseHex("#a8a8a8");

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
            // ── 1. 배경 그라디언트 (위젯 영역 y=0..166만) ────────────────────
            // CSS: linear-gradient(130deg, #3b4351, #282e39)
            // 130deg ≈ top-left → bottom-right 대각선
            ctx.Fill(
                new LinearGradientBrush(
                    new PointF(0, 0), new PointF(W, WidgetH),
                    GradientRepetitionMode.None,
                    new ColorStop(0f, BgStart),
                    new ColorStop(1f, BgEnd)),
                new RectangleF(0, 0, W, WidgetH));
            // y=166..190 은 iframe 배경색 (투명 유지)

            // ── 2. 테두리 (border-top + border-left, 1px #424c5c) ─────────────
            ctx.Fill(BorderClr, new RectangleF(0, 0, W, 1));
            ctx.Fill(BorderClr, new RectangleF(0, 0, 1, WidgetH));

            // ── 3. 헤더: "Buy [Name]" #fefefe + " on Steam" #9e9e9e, 21px ─────
            // h1: font-size=21px, font-weight=normal, display=inline-block, line-height=28px
            // h1.main_text: color=#fefefe, max-width=425px (ellipsis)
            // h1.tail em: color=#9e9e9e, font-weight=300 (Light)
            var titleFont = ff.CreateFont(21, FontStyle.Regular);
            string buyText = $"Buy {data.Name}";
            // max-width: 425px → truncate if needed
            var buySize = TextMeasurer.MeasureSize(buyText, new TextOptions(titleFont));
            while (buySize.Width > 425f && buyText.Length > 7)
            {
                buyText  = buyText[..^4] + "...";
                buySize  = TextMeasurer.MeasureSize(buyText, new TextOptions(titleFont));
            }
            ctx.DrawText(buyText,     titleFont, TitleClr,   new PointF(PadH, PadV));
            ctx.DrawText(" on Steam", titleFont, OnSteamClr, new PointF(PadH + buySize.Width, PadV));

            // ── 4. 캡슐 이미지 (184×69, float:left, margin=2px 10px 10px 0) ──
            using var cimg = Image.Load<Rgba32>(new MemoryStream(capsuleBytes));
            float sc = Math.Max(CapsW / cimg.Width, CapsH / cimg.Height);
            int   sw = (int)Math.Ceiling(cimg.Width  * sc);
            int   sh = (int)Math.Ceiling(cimg.Height * sc);
            cimg.Mutate(c => c.Resize(sw, sh));
            int cropX = (sw - (int)CapsW) / 2;
            int cropY = (sh - (int)CapsH) / 2;
            if (sw > (int)CapsW || sh > (int)CapsH)
                cimg.Mutate(c => c.Crop(new Rectangle(cropX, cropY, (int)CapsW, (int)CapsH)));
            ctx.DrawImage(cimg, new Point((int)CapsX, (int)CapsY), 1f);

            // ── 5. 설명 텍스트 (.desc, 13px #c9c9c9, line-height 16px) ────────
            var descFont = ff.CreateFont(13, FontStyle.Regular);
            ctx.DrawText(
                new RichTextOptions(descFont)
                {
                    Origin         = new PointF(TextX, DescY),
                    WrappingLength = TextW,
                    LineSpacing    = 16f / 13f,   // line-height: 16px ÷ font-size: 13px
                },
                data.ShortDescription, DescClr);

            // ── 6. 플랫폼 아이콘 (absolute bottom=13, left=15) ────────────────
            // Windows 4-square 아이콘 (11×11px total: sq=5, gap=1)
            const float sq = 5f, gap = 1f, iconH = sq + gap + sq;  // 11px
            float iconTop = PlatIconBotY - iconH;                   // 153 - 11 = 142
            ctx.Fill(PlatClr, new RectangleF(PadH,            iconTop,       sq, sq));
            ctx.Fill(PlatClr, new RectangleF(PadH + sq + gap, iconTop,       sq, sq));
            ctx.Fill(PlatClr, new RectangleF(PadH,            iconTop+sq+gap, sq, sq));
            ctx.Fill(PlatClr, new RectangleF(PadH + sq + gap, iconTop+sq+gap, sq, sq));

            // ── 7. 구매 영역 ───────────────────────────────────────────────────
            RenderPurchaseArea(ctx, ff, data);
        });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await card.SaveAsPngAsync(outputPath, ct);
    }

    // ── 구매 영역 (.game_purchase_action_bg + 자식들) ─────────────────────────
    private static void RenderPurchaseArea(
        IImageProcessingContext ctx, FontFamily ff,
        SteamGameData data)
    {
        // Buy on Steam 버튼: font-size=14px, padding=0 11px, line-height=32px
        const string btnLabel = "Buy on Steam";
        var  btnFont  = ff.CreateFont(14, FontStyle.Regular);
        var  btnTSize = TextMeasurer.MeasureSize(btnLabel, new TextOptions(btnFont));
        float btnW    = btnTSize.Width + 22f;   // 11px × 2

        float totalW, startX;

        if (data.DiscountPct > 0)
        {
            // ── discount_pct: font-size=25px, font-weight=500, padding=0 6px, h=32 ──
            string pctLabel = $"-{data.DiscountPct}%";
            var    pctFont  = ff.CreateFont(25, FontStyle.Bold);
            var    pctTSize = TextMeasurer.MeasureSize(pctLabel, new TextOptions(pctFont));
            float  pctW     = pctTSize.Width + 12f;   // 6px × 2

            // ── discount_prices: justify-content=center, align-items=end ─────
            // original: font-size=11px, line-height=12px, color=#738895
            // final:    font-size=15px, line-height=16px, color=#BEEE11  (store.css)
            var  origFont  = ff.CreateFont(11, FontStyle.Regular);
            var  finFont   = ff.CreateFont(15, FontStyle.Regular);
            var  origTSize = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(origFont));
            var  finTSize  = TextMeasurer.MeasureSize(data.Price,         new TextOptions(finFont));
            // prices block width: 4px left-pad + max content + 8px right-pad
            float pricesContentW = Math.Max(origTSize.Width, finTSize.Width);
            float pricesW        = 4f + pricesContentW + 8f;

            totalW = pctW + pricesW + btnW;
            startX = PurchBlockRight - totalW;

            // 흑색 컨테이너 (.game_purchase_action_bg: bg=#000, padding=2 2 2 0)
            ctx.Fill(PurchBg,
                new RectangleF(startX, PurchContY, totalW + 2f, PurchContH));

            // discount_pct 블록 (bg=#4c6b22, color=#BEEE11)
            float pctX = startX;
            ctx.Fill(DiscPctBg, new RectangleF(pctX, PurchBlockY, pctW, PurchBlockH));
            // line-height: 32px → text vertically centered
            float pctTextY = PurchBlockY + (PurchBlockH - pctTSize.Height) / 2f;
            ctx.DrawText(pctLabel, pctFont, DiscPctFg, new PointF(pctX + 6f, pctTextY));

            // discount_prices 블록 (bg=#344654)
            float pricesX = pctX + pctW;
            ctx.Fill(DiscPriceBg, new RectangleF(pricesX, PurchBlockY, pricesW, PurchBlockH));
            // justify-content: center → 두 줄 합계 높이를 32px 내 수직 중앙
            float lineBlock = 12f + 16f;   // orig line-height + final line-height = 28px
            float lineTopY  = PurchBlockY + (PurchBlockH - lineBlock) / 2f;  // 2px offset
            // align-items: end → 우측 정렬 (prices block 오른쪽 - right-pad)
            float origX = pricesX + pricesW - 8f - origTSize.Width;
            float finX  = pricesX + pricesW - 8f - finTSize.Width;
            ctx.DrawText(data.OriginalPrice, origFont, OrigPriceClr, new PointF(origX, lineTopY));
            // 취소선 (::before border-bottom, skewY 생략 → 수평선으로 대체)
            ctx.Fill(OrigPriceClr,
                new RectangleF(origX, lineTopY + 6f - 0.75f, origTSize.Width, 1.5f));
            ctx.DrawText(data.Price, finFont, FinPriceClr, new PointF(finX, lineTopY + 12f));

            // Buy on Steam 버튼
            float btnX = pricesX + pricesW;
            RenderBuyButton(ctx, ff, btnFont, btnLabel, btnX, btnW);
        }
        else
        {
            // 할인 없음: 가격 표시 + 버튼
            var  priceFont  = ff.CreateFont(15, FontStyle.Regular);
            var  priceTSize = TextMeasurer.MeasureSize(data.Price, new TextOptions(priceFont));
            float priceW    = priceTSize.Width + 16f;
            totalW = priceW + btnW;
            startX = PurchBlockRight - totalW;

            ctx.Fill(PurchBg,
                new RectangleF(startX, PurchContY, totalW + 2f, PurchContH));
            ctx.Fill(DiscPriceBg,
                new RectangleF(startX, PurchBlockY, priceW, PurchBlockH));

            float priceTextY = PurchBlockY + (PurchBlockH - priceTSize.Height) / 2f;
            ctx.DrawText(data.Price, priceFont, FinPriceClr,
                new PointF(startX + 8f, priceTextY));

            RenderBuyButton(ctx, ff, btnFont, btnLabel, startX + priceW, btnW);
        }
    }

    private static void RenderBuyButton(
        IImageProcessingContext ctx, FontFamily ff,
        Font btnFont, string label,
        float x, float w)
    {
        // linear-gradient(to right, #6fa720 5%, #588a1b 95%)
        ctx.Fill(
            new LinearGradientBrush(
                new PointF(x, PurchBlockY), new PointF(x + w, PurchBlockY),
                GradientRepetitionMode.None,
                new ColorStop(0.05f, CartBgL),
                new ColorStop(0.95f, CartBgR)),
            new RectangleF(x, PurchBlockY, w, PurchBlockH));

        ctx.DrawText(
            new RichTextOptions(btnFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Origin = new PointF(x + w / 2f, PurchBlockY + PurchBlockH / 2f),
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
