using UnityEngine;
using BloodshedModToolkit.Combat;

namespace BloodshedModToolkit.UI.Overlay.Panels
{
    /// <summary>
    /// DPS 미터 패널 — 전투 중 우측 상단에 DPS / 피크 / 누적 피해를 표시합니다.
    /// 전투 공백 후 1.5초에 걸쳐 페이드아웃됩니다.
    ///
    /// Height 는 UIContext 측정 모드(drawing=false)로 DrawContent 를 실행하여
    /// 실제 레이아웃 높이를 자동으로 계산합니다 — 매직 상수 없음.
    /// </summary>
    public sealed class DpsPanel : IOverlayPanel
    {
        public string     Id    => "dps";
        public EdgeInsets Padding => EdgeInsets.All(OverlayManager.Margin);

        private float _alpha;

        public bool  Visible         => _alpha >= 0.02f;
        public float BackgroundAlpha => 0.72f * _alpha;

        /// <summary>
        /// UIContext 측정 모드로 DrawContent 를 실행하여 패딩 포함 총 높이를 반환합니다.
        /// DrawContent 의 행 높이/간격이 변경되면 이 값도 자동으로 따라갑니다.
        /// OverlayStyle.EnsureReady() 이후에만 정확한 값 반환 (OverlayManager.Draw 에서 보장).
        /// </summary>
        public float Height
        {
            get
            {
                var ctx = new UIContext(0f, 0f, OverlayManager.PanelWidth,
                                       Padding, drawing: false);
                DrawContent(ctx, 1f);   // alpha 무관 — GUI 호출 없음
                return ctx.TotalHeight;
            }
        }

        // ── IOverlayPanel ───────────────────────────────────────────────────
        public void Tick()
        {
            DpsTracker.Tick();

            float sinceHit = DpsTracker.TimeSinceHit;
            if (DpsTracker.IsActive)
                _alpha = Mathf.Clamp01(sinceHit < 0.3f ? sinceHit / 0.3f : 1f); // 0.3초 페이드인
            else
            {
                float t = (sinceHit - DpsTracker.CombatGap) / 1.5f;
                _alpha = Mathf.Clamp01(1f - t);                                  // 1.5초 페이드아웃
            }
        }

        public void Draw(UIContext ctx) => DrawContent(ctx, _alpha);

        // ── 레이아웃 ────────────────────────────────────────────────────────
        /// <summary>
        /// 공통 레이아웃 메서드. measure / draw 두 모드에서 동일하게 호출됩니다.
        ///
        /// 행 높이는 OverlayStyle.XxxLineH (CalcHeight 기반) 로 런타임 폰트 메트릭에서
        /// 자동 계산됩니다 — 하드코딩 없음, 클리핑 없음.
        ///
        /// 행 구성:
        ///   Row 1 — 타이틀 "◈  DPS"       11pt Bold    height=TitleLineH   gap=2
        ///   Row 2 — DPS 숫자               22pt Bold    height=DpsNumLineH  gap=2
        ///   Row 3 — 게이지 바              5px          height=5            gap=5
        ///   Row 4 — 피크 / 히트 수 / 누적  10pt Normal  height=SmallLineH   gap=0
        /// </summary>
        private void DrawContent(UIContext ctx, float alpha)
        {
            float dps    = DpsTracker.CurrentDps;
            float peak   = Mathf.Max(DpsTracker.ValidPeakDps, dps, 1f);
            Color dpsCol = DpsColor(dps);

            // 히트 직후 0.12초간 흰색으로 플래시
            float flash  = Mathf.Clamp01(1f - DpsTracker.TimeSinceHit / 0.12f);
            Color numCol = Color.Lerp(dpsCol, Color.white, flash * 0.65f);

            float  pk  = DpsTracker.ValidPeakDps;
            string sub = pk > 0f
                ? $"\u25b2pk {FormatDps(pk)}   {DpsTracker.HitCount} hits   tot {FormatTotal(DpsTracker.TotalDamage)}"
                : $"{DpsTracker.HitCount} hits   total {FormatTotal(DpsTracker.TotalDamage)}";

            ctx
                // Row 1: ◈ DPS 타이틀 — TitleLineH 로 클리핑 없이 렌더링
                .Label(new Color(OverlayStyle.Amber.r, OverlayStyle.Amber.g,
                                 OverlayStyle.Amber.b, alpha),
                       "\u25c8  DPS", OverlayStyle.Title!, height: OverlayStyle.TitleLineH, gap: 2f)

                // Row 2: DPS 숫자 (22pt Bold, 우측 정렬) — DpsNumLineH 로 클리핑 없이 렌더링
                .Label(new Color(numCol.r, numCol.g, numCol.b, alpha),
                       FormatDps(dps), OverlayStyle.DpsNum!, height: OverlayStyle.DpsNumLineH, gap: 2f)

                // Row 3: 게이지 바 (고정 5px)
                .Bar(dps / peak, dpsCol, alpha, height: 5f, gap: 5f)

                // Row 4: 피크 / 히트 수 / 누적 피해 — SmallLineH 로 클리핑 없이 렌더링
                .Label(new Color(OverlayStyle.Dim.r, OverlayStyle.Dim.g,
                                 OverlayStyle.Dim.b, alpha),
                       sub, OverlayStyle.Small!, height: OverlayStyle.SmallLineH, gap: 0f);
        }

        // ── 헬퍼 ────────────────────────────────────────────────────────────
        /// <summary>DPS 크기에 따른 색상. gray → lime → yellow → orange → red.</summary>
        private static Color DpsColor(float dps)
        {
            if (dps <  200f) return new Color(0.55f, 0.55f, 0.55f);  // 회색
            if (dps < 1000f) return new Color(0.32f, 1.00f, 0.28f);  // 라임
            if (dps < 3000f) return new Color(1.00f, 0.86f, 0.08f);  // 노랑
            if (dps < 7000f) return new Color(1.00f, 0.42f, 0.05f);  // 주황
            return                  new Color(1.00f, 0.16f, 0.16f);  // 빨강
        }

        private static string FormatDps(float v)
        {
            if (v <  1000f) return ((int)v).ToString();
            if (v < 10000f) return (v / 1000f).ToString("F1") + "K";
            return           ((int)(v / 1000f)).ToString() + "K";
        }

        private static string FormatTotal(float v)
        {
            if (v <    1000f) return ((int)v).ToString();
            if (v < 1000000f) return (v / 1000f).ToString("F1") + "K";
            return             (v / 1000000f).ToString("F1") + "M";
        }
    }
}
