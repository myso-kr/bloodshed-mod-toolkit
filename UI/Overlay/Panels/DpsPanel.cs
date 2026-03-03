using UnityEngine;
using BloodshedModToolkit.Combat;

namespace BloodshedModToolkit.UI.Overlay.Panels
{
    /// <summary>
    /// DPS 미터 패널 — 전투 중 우측 상단에 DPS / 피크 / 누적 피해를 표시합니다.
    /// 전투 공백 후 1.5초에 걸쳐 페이드아웃됩니다.
    /// </summary>
    public sealed class DpsPanel : IOverlayPanel
    {
        public string Id => "dps";

        private float _alpha;

        public bool Visible => _alpha >= 0.02f;

        public float Height => 76f;

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

        public void Draw(Rect rect)
        {
            if (_alpha < 0.02f) return;

            var saved = GUI.color;

            float cx = rect.x + OverlayManager.Margin;
            float cy = rect.y + OverlayManager.Margin;
            float cw = rect.width - OverlayManager.Margin * 2;

            float dps    = DpsTracker.CurrentDps;
            float peak   = Mathf.Max(DpsTracker.ValidPeakDps, dps, 1f);
            Color dpsCol = DpsColor(dps);

            // 히트 직후 0.12초간 흰색으로 플래시
            float flash  = Mathf.Clamp01(1f - DpsTracker.TimeSinceHit / 0.12f);
            Color numCol = Color.Lerp(dpsCol, Color.white, flash * 0.65f);

            // 배경
            OverlayStyle.DrawBg(rect, 0.72f * _alpha);

            // ── Row 1: ◈ DPS ─────────────────────────────────────────────────
            GUI.color = new Color(OverlayStyle.Amber.r, OverlayStyle.Amber.g,
                                  OverlayStyle.Amber.b, _alpha);
            GUI.Label(new Rect(cx, cy, cw, 12), "\u25c8  DPS", OverlayStyle.Title!);
            cy += 14f;

            // ── Row 2: DPS 숫자 (22pt, 우측 정렬) ────────────────────────────
            GUI.color = new Color(numCol.r, numCol.g, numCol.b, _alpha);
            GUI.Label(new Rect(cx, cy, cw, 24), FormatDps(dps), OverlayStyle.DpsNum!);
            cy += 26f;

            // ── Row 3: 게이지 바 ─────────────────────────────────────────────
            OverlayStyle.DrawBar(new Rect(cx, cy, cw, 5), dps / peak, dpsCol, _alpha);
            cy += 7f;

            // ── Row 4: 피크 / 히트 수 / 누적 피해 ───────────────────────────
            float pk   = DpsTracker.ValidPeakDps;
            string sub = pk > 0f
                ? $"\u25b2{FormatDps(pk)}   \u25cf{DpsTracker.HitCount}H   \u2295{FormatTotal(DpsTracker.TotalDamage)}"
                : $"\u25cf{DpsTracker.HitCount}H   \u2295{FormatTotal(DpsTracker.TotalDamage)}";
            GUI.color = new Color(OverlayStyle.Dim.r, OverlayStyle.Dim.g,
                                  OverlayStyle.Dim.b, _alpha);
            GUI.Label(new Rect(cx, cy, cw, 12), sub, OverlayStyle.Small!);

            GUI.color = saved;
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
