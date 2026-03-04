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
///   [#16202d 패널 | 231×87 캡슐 이미지]  [게임명 / 가격 / 할인 / 리뷰]
///   [#131a21 푸터 바 — "Available on Steam  ·  store.steampowered.com"]
///
/// 색상: Steam 다크 테마 (#1b2838 계열)
/// </summary>
public static class CardRenderer
{
    // ── 캔버스 크기 ──────────────────────────────────────────────────────────
    private const int W = 640;
    private const int H = 190;

    // ── Steam 다크 팔레트 ─────────────────────────────────────────────────────
    private static readonly Color BgMain        = Color.ParseHex("#1b2838");
    private static readonly Color BgPanel       = Color.ParseHex("#16202d");
    private static readonly Color BgFooter      = Color.ParseHex("#131a21");
    private static readonly Color Separator     = Color.ParseHex("#2a475e");
    private static readonly Color AccentBlue    = Color.ParseHex("#66c0f4");
    private static readonly Color DiscountBg    = Color.ParseHex("#4c6b22");
    private static readonly Color DiscountFg    = Color.ParseHex("#a4d007");
    private static readonly Color TextPrimary   = Color.ParseHex("#c6d4df");
    private static readonly Color TextSecondary = Color.ParseHex("#acb2b8");
    private static readonly Color TextDim       = Color.ParseHex("#67707b");
    private static readonly Color ReviewPos     = Color.ParseHex("#66c0f4");
    private static readonly Color ReviewMix     = Color.ParseHex("#b9a074");
    private static readonly Color ReviewNeg     = Color.ParseHex("#c24d2c");

    // ── 레이아웃 상수 ─────────────────────────────────────────────────────────
    private const int Pad      = 14;
    // header.jpg 460×215 → 패널에 꽉 차게 스케일: 높이=190 → 너비=460/215*190≈406, 중앙 크롭
    private const int PanelW   = 230;              // 좌측 이미지 패널 너비
    private const int TextX    = PanelW + 1;       // 구분선 바로 오른쪽
    private const int TextPad  = Pad;
    private const int TextMaxW = W - TextX - TextPad - Pad; // 오른쪽 여백 포함
    private const int FooterY  = H - 32;
    private const int FooterH  = 32;

    public static async Task RenderAsync(
        SteamGameData data,
        byte[]        capsuleBytes,
        string        outputPath,
        CancellationToken ct = default)
    {
        FontFamily ff = ResolveFont();
        using var image = new Image<Rgba32>(W, H);

        image.Mutate(ctx =>
        {
            // ── 1. 배경 ──────────────────────────────────────────────────────
            ctx.Fill(BgMain);

            // ── 2. 좌측 패널 — header.jpg 를 패널 전체에 꽉 채워 크롭 ────────
            using var headerStream = new MemoryStream(capsuleBytes);
            using var header = Image.Load<Rgba32>(headerStream);
            // 높이를 H에 맞추고 폭을 비율 유지 → 좌측 크롭으로 패널 채우기
            int scaledW = (int)Math.Ceiling(header.Width * (double)H / header.Height);
            header.Mutate(c =>
            {
                c.Resize(scaledW, H);
                // 오른쪽 여백 크롭 (왼쪽 정렬이 게임 로고를 더 잘 보이게 함)
                if (scaledW > PanelW)
                    c.Crop(new Rectangle(0, 0, PanelW, H));
            });
            ctx.DrawImage(header, new Point(0, 0), opacity: 1f);

            // 이미지 위에 좌측에서 오른쪽으로 그라디언트 오버레이 (구분선 효과)
            // 단순히 오른쪽 끝에 얇은 어두운 페이드
            ctx.Fill(Color.FromRgba(0x13, 0x1a, 0x21, 180),
                     new RectangleF(PanelW - 30, 0, 30, H));

            // ── 3. 구분선 ─────────────────────────────────────────────────────
            ctx.Fill(Separator, new RectangleF(PanelW, 0, 1, H));

            // ── 5. 우측 텍스트 배경 (살짝 어둡게) ────────────────────────────
            ctx.Fill(Color.FromRgba(0x1b, 0x28, 0x38, 230),
                     new RectangleF(TextX + 1, 0, W - TextX - 1, H));

            float tx = TextX + TextPad;  // 텍스트 시작 x

            // ── 6. 게임명 ─────────────────────────────────────────────────────
            DrawText(ctx, ff, 17, bold: true,
                     data.Name,
                     TextPrimary,
                     tx, Pad);

            // ── 7. 얇은 구분선 ───────────────────────────────────────────────
            int ruleY = Pad + 26;
            ctx.Fill(Separator, new RectangleF(tx, ruleY, TextMaxW, 1));

            // ── 8. 가격 행 ───────────────────────────────────────────────────
            int priceY = ruleY + 10;

            if (data.DiscountPct > 0)
            {
                // [-XX%] 배지
                string badge = $" -{data.DiscountPct}% ";
                var badgeFont = ff.CreateFont(11, FontStyle.Bold);
                FontRectangle badgeSize = TextMeasurer.MeasureSize(badge, new TextOptions(badgeFont));
                float bx = tx, by = priceY - 1f;
                float bw = badgeSize.Width + 2, bh = badgeSize.Height + 2;
                ctx.Fill(DiscountBg, new RectangleF(bx, by, bw, bh));
                DrawText(ctx, ff, 11, bold: true, badge, DiscountFg, bx + 1, by + 1);

                // 취소선 원가
                float strikeX = bx + bw + 6;
                var origFont = ff.CreateFont(11, FontStyle.Regular);
                FontRectangle origSize = TextMeasurer.MeasureSize(data.OriginalPrice, new TextOptions(origFont));
                DrawText(ctx, ff, 11, false, data.OriginalPrice, TextDim, strikeX, priceY + 1);
                float strikeY2 = priceY + origSize.Height / 2f + 1;
                ctx.Fill(TextDim, new RectangleF(strikeX, strikeY2, origSize.Width, 1));

                // 할인가 (강조)
                float finalX = strikeX + origSize.Width + 8;
                DrawText(ctx, ff, 15, bold: true, data.Price, AccentBlue, finalX, priceY - 1);
            }
            else
            {
                DrawText(ctx, ff, 15, bold: true, data.Price, AccentBlue, tx, priceY - 1);
            }

            // ── 9. 리뷰 행 ───────────────────────────────────────────────────
            int reviewY = priceY + 28;

            Color rColor = data.ReviewPct >= 70 ? ReviewPos
                         : data.ReviewPct >= 40 ? ReviewMix
                         :                        ReviewNeg;

            string star = data.ReviewPct >= 70 ? "★" : data.ReviewPct >= 40 ? "◆" : "✖";
            DrawText(ctx, ff, 13, bold: true,
                     $"{star}  {data.ReviewPct}% Positive",
                     rColor, tx, reviewY);

            DrawText(ctx, ff, 11, false,
                     $"{data.ReviewLabel}  ({data.TotalReviews:N0} reviews)",
                     TextDim, tx, reviewY + 18);

            // ── 9. 푸터 바 ───────────────────────────────────────────────────
            ctx.Fill(BgFooter, new RectangleF(0, FooterY, W, FooterH));
            ctx.Fill(Separator, new RectangleF(0, FooterY, W, 1));

            DrawText(ctx, ff, 10, false,
                     "Available on Steam  ·  Prices may vary by region  ·  Reviews sourced from Steam",
                     TextDim,
                     W / 2f, FooterY + FooterH / 2f - 5f,
                     center: true);
        });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await image.SaveAsPngAsync(outputPath, ct);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static FontFamily ResolveFont()
    {
        // Windows: Segoe UI / Arial  |  Linux: DejaVu / Liberation
        string[] candidates = ["Segoe UI", "Roboto", "Arial", "DejaVu Sans",
                                "Liberation Sans", "Helvetica Neue", "Helvetica"];
        foreach (var name in candidates)
            if (SystemFonts.TryGet(name, out FontFamily ff))
                return ff;

        return SystemFonts.Collection.Families.First();
    }

    private static void DrawText(
        IImageProcessingContext ctx,
        FontFamily ff,
        float size,
        bool bold,
        string text,
        Color color,
        float x, float y,
        bool center = false)
    {
        var font = ff.CreateFont(size, bold ? FontStyle.Bold : FontStyle.Regular);

        if (center)
        {
            FontRectangle measured = TextMeasurer.MeasureSize(text, new TextOptions(font));
            x -= measured.Width / 2f;
        }

        ctx.DrawText(text, font, color, new PointF(x, y));
    }
}
