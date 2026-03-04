using System.Text;
using SixLabors.Fonts;

namespace SteamCard;

/// <summary>
/// Steam 위젯 (646×190) 을 SVG로 재현합니다.
/// - 캡슐 이미지는 base64 data URI로 임베드 → CORS 없음
/// - 텍스트 레이아웃 계산에만 SixLabors.Fonts 사용
/// - 실제 텍스트 렌더링은 브라우저 SVG 엔진 → 폰트 메트릭 100% 정확
///
/// 레이아웃 출처: styles_widget.css + store.css
/// </summary>
public static class CardRenderer
{
    // ── 캔버스 크기 ────────────────────────────────────────────────────────────
    private const int W = 646;
    private const int H = 190;

    // ── 위젯 레이아웃 상수 (styles_widget.css) ─────────────────────────────────
    // #widget: height=146px content-box, padding=10px 15px → DOM h=166px
    private const float WidgetH    = 166f;
    private const float PadH       = 15f;   // 좌우 패딩
    private const float PadV       = 10f;   // 상하 패딩
    private const float HeaderH    = 28f;   // h1 line-height

    // .capsule: float:left, margin=2px 10px 10px 0, w=184, h=69
    private const float CapsW      = 184f;
    private const float CapsH      = 69f;

    // .desc: margin-top=10px → y=48; capsule margin-top=2px → capsule y=50
    private const float DescY      = PadV + HeaderH + 10f;          // 48
    private const float CapsX      = PadH;                           // 15
    private const float CapsY      = DescY + 2f;                     // 50
    private const float TextX      = CapsX + CapsW + 10f;           // 209
    private const float TextW      = W - PadH - 10f - TextX;        // 412

    // .game_area_purchase_platform: absolute, bottom=13px, left=15px
    private const float PlatIconBotY = WidgetH - 13f;               // 153

    // .game_purchase_action: absolute, bottom=-20px, right=10px
    //   → element bottom = WidgetH + 20 = 186
    //   → .game_purchase_action_bg: h=32 content + padding=2 2 2 0 → total=36
    //   → container top = 186 - 36 = 150
    //   → blocks y=152 (top+2pad), h=32
    //   → content right = W-10-2 = 634
    private const float PurchContY      = 150f;
    private const float PurchContH      = 36f;
    private const float PurchBlockY     = 152f;
    private const float PurchBlockH     = 32f;
    private const float PurchBlockMid   = PurchBlockY + PurchBlockH / 2f;  // 168
    private const float PurchBlockRight = 634f;

    // ── 공개 API ───────────────────────────────────────────────────────────────
    public static async Task RenderAsync(
        SteamGameData data,
        byte[]        capsuleBytes,
        string        outputPath,
        CancellationToken ct = default)
    {
        var ff          = ResolveFont();
        var capsBase64  = Convert.ToBase64String(capsuleBytes);

        var sb = new StringBuilder(64_000);
        BuildSvg(sb, ff, data, capsBase64);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, ct);
    }

    // ── SVG 빌더 ───────────────────────────────────────────────────────────────
    private static void BuildSvg(
        StringBuilder sb, FontFamily ff,
        SteamGameData data, string capsBase64)
    {
        // 구매 영역 레이아웃을 먼저 계산 (btnGrad의 x 좌표에 필요)
        var pu = CalcPurchase(ff, data);

        // ── SVG 루트 ──────────────────────────────────────────────────────────
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">""");

        // ── defs ──────────────────────────────────────────────────────────────
        sb.AppendLine("  <defs>");

        // 배경 그라디언트 — CSS: linear-gradient(130deg, #3b4351, #282e39)
        // SVG userSpaceOnUse: 방향 벡터 sin(130°)=0.766, -cos(130°)=0.643
        // 중심(323,83)에서 ±340 떨어진 두 점
        sb.AppendLine("""    <linearGradient id="bg" gradientUnits="userSpaceOnUse" x1="63" y1="-135" x2="583" y2="301">""");
        sb.AppendLine("""      <stop offset="0%" stop-color="#3b4351"/>""");
        sb.AppendLine("""      <stop offset="100%" stop-color="#282e39"/>""");
        sb.AppendLine("    </linearGradient>");

        // 버튼 그라디언트 — CSS: linear-gradient(to right, #6fa720 5%, #588a1b 95%)
        sb.AppendLine($"""    <linearGradient id="btn" gradientUnits="userSpaceOnUse" x1="{pu.BtnX:F0}" y1="0" x2="{pu.BtnX + pu.BtnW:F0}" y2="0">""");
        sb.AppendLine("""      <stop offset="5%" stop-color="#6fa720"/>""");
        sb.AppendLine("""      <stop offset="95%" stop-color="#588a1b"/>""");
        sb.AppendLine("    </linearGradient>");

        // 캡슐 이미지 클립패스
        sb.AppendLine($"""    <clipPath id="cap"><rect x="{CapsX:F0}" y="{CapsY:F0}" width="{CapsW:F0}" height="{CapsH:F0}"/></clipPath>""");

        sb.AppendLine("  </defs>");

        // ── 배경 + 테두리 (border-top, border-left 각 1px) ────────────────────
        sb.AppendLine($"""  <rect width="{W}" height="{WidgetH:F0}" fill="url(#bg)"/>""");
        sb.AppendLine($"""  <rect width="{W}" height="1" fill="#424c5c"/>""");
        sb.AppendLine($"""  <rect width="1" height="{WidgetH:F0}" fill="#424c5c"/>""");

        // ── 헤더: "Buy [Name]" #fefefe + " on Steam" #9e9e9e ─────────────────
        // h1: font-size=21px, line-height=28px, .header_tail: font-weight=300
        string buyText = TruncateText($"Buy {data.Name}", ff, 21, FontStyle.Regular, 425f);
        sb.AppendLine($"""  <text x="{PadH:F0}" y="{PadV:F0}" font-family="Segoe UI, Arial, sans-serif" font-size="21" dominant-baseline="hanging"><tspan fill="#fefefe">{Enc(buyText)}</tspan><tspan fill="#9e9e9e" font-weight="300"> on Steam</tspan></text>""");

        // ── 캡슐 이미지 (cover/crop: preserveAspectRatio=xMidYMid slice) ──────
        sb.AppendLine($"""  <image x="{CapsX:F0}" y="{CapsY:F0}" width="{CapsW:F0}" height="{CapsH:F0}" href="data:image/jpeg;base64,{capsBase64}" preserveAspectRatio="xMidYMid slice" clip-path="url(#cap)"/>""");

        // ── 설명 텍스트 (word-wrapped, 13px, line-height=16px) ────────────────
        var lines = WrapText(data.ShortDescription, ff, 13, FontStyle.Regular, TextW);
        // 플랫폼 아이콘 위까지만 표시
        int maxLines = (int)Math.Floor((PlatIconBotY - 11f - DescY) / 16f);
        if (lines.Count > maxLines) lines = lines.Take(maxLines).ToList();

        if (lines.Count > 0)
        {
            sb.AppendLine("""  <text font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#c9c9c9" dominant-baseline="hanging">""");
            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 0)
                    sb.AppendLine($"""    <tspan x="{TextX:F0}" y="{DescY:F0}">{Enc(lines[i])}</tspan>""");
                else
                    sb.AppendLine($"""    <tspan x="{TextX:F0}" dy="16">{Enc(lines[i])}</tspan>""");
            }
            sb.AppendLine("  </text>");
        }

        // ── 플랫폼 아이콘 (Windows 4-square, bottom=13px → top=142) ──────────
        float it = PlatIconBotY - 11f;  // 142
        sb.AppendLine($"""  <rect x="{PadH:F0}"       y="{it:F0}"     width="5" height="5" fill="#a8a8a8"/>""");
        sb.AppendLine($"""  <rect x="{PadH + 6f:F0}"  y="{it:F0}"     width="5" height="5" fill="#a8a8a8"/>""");
        sb.AppendLine($"""  <rect x="{PadH:F0}"       y="{it + 6f:F0}" width="5" height="5" fill="#a8a8a8"/>""");
        sb.AppendLine($"""  <rect x="{PadH + 6f:F0}"  y="{it + 6f:F0}" width="5" height="5" fill="#a8a8a8"/>""");

        // ── 구매 영역 ─────────────────────────────────────────────────────────
        BuildPurchaseSvg(sb, data, pu);

        sb.AppendLine("</svg>");
    }

    // ── 구매 영역 레이아웃 계산 ────────────────────────────────────────────────
    private record PurchaseLayout(
        float StartX, float TotalW,
        float BtnX,    float BtnW,
        float PctX,    float PctW,
        float PricesX, float PricesW,
        bool  HasDiscount);

    private static PurchaseLayout CalcPurchase(FontFamily ff, SteamGameData data)
    {
        var   btnFont = ff.CreateFont(14, FontStyle.Regular);
        float btnTW   = TextMeasurer.MeasureSize("Buy on Steam", new TextOptions(btnFont)).Width;
        float btnW    = btnTW + 22f;  // padding: 0 11px

        if (data.DiscountPct > 0)
        {
            string pctLabel = $"-{data.DiscountPct}%";
            var    pctFont  = ff.CreateFont(25, FontStyle.Bold);
            float  pctTW    = TextMeasurer.MeasureSize(pctLabel, new TextOptions(pctFont)).Width;
            float  pctW     = pctTW + 12f;  // padding: 0 6px

            var   origFont = ff.CreateFont(11, FontStyle.Regular);
            var   finFont  = ff.CreateFont(15, FontStyle.Regular);
            float origTW   = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(origFont)).Width;
            float finTW    = TextMeasurer.MeasureSize(data.Price,         new TextOptions(finFont)).Width;
            float pricesW  = 4f + MathF.Max(origTW, finTW) + 8f;  // 4px left + content + 8px right

            float totalW = pctW + pricesW + btnW;
            float startX = PurchBlockRight - totalW;
            return new PurchaseLayout(
                startX, totalW,
                startX + pctW + pricesW, btnW,
                startX, pctW,
                startX + pctW, pricesW,
                true);
        }
        else
        {
            var   priceFont = ff.CreateFont(15, FontStyle.Regular);
            float priceTW   = TextMeasurer.MeasureSize(data.Price, new TextOptions(priceFont)).Width;
            float priceW    = priceTW + 16f;  // padding: 0 8px
            float totalW    = priceW + btnW;
            float startX    = PurchBlockRight - totalW;
            return new PurchaseLayout(
                startX, totalW,
                startX + priceW, btnW,
                0f, 0f,
                startX, priceW,
                false);
        }
    }

    // ── 구매 영역 SVG 출력 ────────────────────────────────────────────────────
    private static void BuildPurchaseSvg(
        StringBuilder sb, SteamGameData data, PurchaseLayout pu)
    {
        // 검정 컨테이너 (.game_purchase_action_bg)
        sb.AppendLine($"""  <rect x="{pu.StartX:F0}" y="{PurchContY:F0}" width="{pu.TotalW + 2f:F0}" height="{PurchContH:F0}" fill="#000"/>""");

        if (pu.HasDiscount)
        {
            // 할인율 블록 (bg=#4c6b22, fg=#BEEE11, 25px bold)
            sb.AppendLine($"""  <rect x="{pu.PctX:F0}" y="{PurchBlockY:F0}" width="{pu.PctW:F0}" height="{PurchBlockH:F0}" fill="#4c6b22"/>""");
            sb.AppendLine($"""  <text x="{pu.PctX + pu.PctW / 2f:F0}" y="{PurchBlockMid:F0}" text-anchor="middle" dominant-baseline="middle" font-family="Segoe UI, Arial, sans-serif" font-size="25" font-weight="bold" fill="#BEEE11">-{data.DiscountPct}%</text>""");

            // 가격 블록 (bg=#344654)
            // 두 줄 (orig 11px line-height=12, final 15px line-height=16) 합=28px
            // 수직 중앙: lineTopY = 152 + (32-28)/2 = 154
            float lineTopY = PurchBlockY + (PurchBlockH - 28f) / 2f;  // 154
            float rightX   = pu.PricesX + pu.PricesW - 8f;

            sb.AppendLine($"""  <rect x="{pu.PricesX:F0}" y="{PurchBlockY:F0}" width="{pu.PricesW:F0}" height="{PurchBlockH:F0}" fill="#344654"/>""");
            // 원래 가격 (취소선, 11px, #738895)
            sb.AppendLine($"""  <text x="{rightX:F0}" y="{lineTopY:F0}" text-anchor="end" dominant-baseline="hanging" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#738895" text-decoration="line-through">{Enc(data.OriginalPrice)}</text>""");
            // 최종 가격 (15px, #BEEE11)
            sb.AppendLine($"""  <text x="{rightX:F0}" y="{lineTopY + 12f:F0}" text-anchor="end" dominant-baseline="hanging" font-family="Segoe UI, Arial, sans-serif" font-size="15" fill="#BEEE11">{Enc(data.Price)}</text>""");
        }
        else
        {
            // 할인 없음: 가격 블록 (bg=#344654, 15px, #BEEE11, 수직 중앙)
            sb.AppendLine($"""  <rect x="{pu.PricesX:F0}" y="{PurchBlockY:F0}" width="{pu.PricesW:F0}" height="{PurchBlockH:F0}" fill="#344654"/>""");
            sb.AppendLine($"""  <text x="{pu.PricesX + pu.PricesW / 2f:F0}" y="{PurchBlockMid:F0}" text-anchor="middle" dominant-baseline="middle" font-family="Segoe UI, Arial, sans-serif" font-size="15" fill="#BEEE11">{Enc(data.Price)}</text>""");
        }

        // Buy on Steam 버튼 (gradient, 14px, #d2efa9)
        sb.AppendLine($"""  <rect x="{pu.BtnX:F0}" y="{PurchBlockY:F0}" width="{pu.BtnW:F0}" height="{PurchBlockH:F0}" fill="url(#btn)"/>""");
        sb.AppendLine($"""  <text x="{pu.BtnX + pu.BtnW / 2f:F0}" y="{PurchBlockMid:F0}" text-anchor="middle" dominant-baseline="middle" font-family="Segoe UI, Arial, sans-serif" font-size="14" fill="#d2efa9">Buy on Steam</text>""");
    }

    // ── 텍스트 유틸리티 ────────────────────────────────────────────────────────

    /// <summary>텍스트가 maxWidth 초과 시 뒤에서 ... 로 잘라냅니다.</summary>
    private static string TruncateText(
        string text, FontFamily ff, float size, FontStyle style, float maxWidth)
    {
        var font = ff.CreateFont(size, style);
        var opts = new TextOptions(font);
        while (TextMeasurer.MeasureSize(text, opts).Width > maxWidth && text.Length > 4)
            text = text[..^4] + "...";
        return text;
    }

    /// <summary>단어 단위 줄바꿈 — 각 줄을 List로 반환합니다.</summary>
    private static List<string> WrapText(
        string text, FontFamily ff, float size, FontStyle style, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var font  = ff.CreateFont(size, style);
        var opts  = new TextOptions(font);
        var lines = new List<string>();
        var cur   = new StringBuilder();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string test = cur.Length == 0 ? word : cur + " " + word;
            if (cur.Length > 0 && TextMeasurer.MeasureSize(test, opts).Width > maxWidth)
            {
                lines.Add(cur.ToString());
                cur.Clear();
            }
            if (cur.Length > 0) cur.Append(' ');
            cur.Append(word);
        }
        if (cur.Length > 0) lines.Add(cur.ToString());
        return lines;
    }

    /// <summary>SVG 출력용 최소 HTML 이스케이프.</summary>
    private static string Enc(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;");

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
