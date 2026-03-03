using UnityEngine;

namespace BloodshedModToolkit.UI.Overlay
{
    /// <summary>
    /// 모든 오버레이 패널이 공유하는 GUIStyle 캐시 및 그리기 유틸리티.
    /// OnGUI 최초 호출 시 EnsureReady() 로 초기화합니다.
    /// 모든 스타일의 textColor 는 white — 실제 색상은 GUI.color 로 착색합니다.
    /// </summary>
    public static class OverlayStyle
    {
        private static bool _ready;

        // ── 공통 GUIStyle ──────────────────────────────────────────────────────
        public static GUIStyle? Title  { get; private set; }  // 11px Bold  — 패널 타이틀
        public static GUIStyle? Item   { get; private set; }  // 10px Normal — 항목 텍스트
        public static GUIStyle? Small  { get; private set; }  // 10px Normal — 보조 정보
        public static GUIStyle? DpsNum { get; private set; }  // 22px Bold, 우측정렬 — DPS 숫자

        // ── 공통 색상 상수 ─────────────────────────────────────────────────────
        public static readonly Color Amber = new Color(1.00f, 0.72f, 0.00f);
        public static readonly Color Lime  = new Color(0.27f, 1.00f, 0.33f);
        public static readonly Color Dim   = new Color(0.58f, 0.58f, 0.58f);

        // ── 초기화 ─────────────────────────────────────────────────────────────
        /// <summary>OnGUI 진입점에서 반드시 호출. 최초 1회만 실제 작업합니다.</summary>
        public static void EnsureReady()
        {
            if (_ready) return;
            _ready = true;

            Title  = Make(11, FontStyle.Bold);
            Item   = Make(10, FontStyle.Normal);
            Small  = Make(10, FontStyle.Normal);
            DpsNum = Make(22, FontStyle.Bold, TextAnchor.MiddleRight);
        }

        private static GUIStyle Make(int size, FontStyle style,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize  = size,
                fontStyle = style,
                wordWrap  = false,
                alignment = anchor,
            };
            s.normal.textColor = Color.white;
            return s;
        }

        // ── 그리기 헬퍼 ────────────────────────────────────────────────────────
        /// <summary>반투명 어두운 배경 박스를 그립니다.</summary>
        public static void DrawBg(Rect rect, float alpha = 0.68f)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>가로 진행 바를 그립니다 (배경 + 채움).</summary>
        public static void DrawBar(Rect rect, float ratio, Color fill, float alpha)
        {
            var prev = GUI.color;

            GUI.color = new Color(0.18f, 0.18f, 0.18f, 0.9f * alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            if (ratio > 0.005f)
            {
                GUI.color = new Color(fill.r, fill.g, fill.b, alpha);
                GUI.DrawTexture(
                    new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height),
                    Texture2D.whiteTexture);
            }

            GUI.color = prev;
        }
    }
}
