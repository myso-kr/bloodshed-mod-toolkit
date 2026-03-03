using UnityEngine;

namespace BloodshedModToolkit.UI.Overlay
{
    /// <summary>
    /// HTML clientRect 에서 영감을 받은 IMGUI 레이아웃 컨텍스트.
    ///
    /// 두 가지 모드:
    ///   - drawing=true  : 실제 GUI 호출로 화면에 렌더링합니다.
    ///   - drawing=false : GUI 호출 없이 커서만 이동하여 TotalHeight 를 계산합니다.
    ///                     IOverlayPanel.Height 구현에 사용합니다.
    ///
    /// 사용 예 — 측정:
    ///   var m = new UIContext(0f, 0f, OverlayManager.PanelWidth, Padding, drawing: false);
    ///   Layout(m);
    ///   float h = m.TotalHeight;
    ///
    /// 사용 예 — 드로우:
    ///   public void Draw(UIContext ctx) => Layout(ctx);
    ///
    /// method chaining 을 지원합니다 (각 메서드가 this 반환).
    /// </summary>
    public sealed class UIContext
    {
        private readonly bool  _drawing;
        private readonly float _originY;   // 패널 상단 Y
        private readonly float _cx;        // 콘텐츠 좌측 X  (panelX + padding.Left)
        private readonly float _cw;        // 콘텐츠 너비    (panelWidth - padding.Horizontal)
        private readonly float _pb;        // 패딩 하단      (padding.Bottom)
        private          float _cursor;    // 현재 커서 Y    (다음 요소 상단)

        // ── 읽기 전용 속성 ─────────────────────────────────────────────────────
        /// <summary>콘텐츠 영역 좌측 X.</summary>
        public float ContentX => _cx;

        /// <summary>콘텐츠 영역 너비.</summary>
        public float ContentW => _cw;

        /// <summary>현재 커서 Y.</summary>
        public float CursorY  => _cursor;

        /// <summary>
        /// 패딩을 포함한 총 높이.
        /// 측정 모드에서 레이아웃 완료 후 읽어 IOverlayPanel.Height 를 결정합니다.
        /// </summary>
        public float TotalHeight => (_cursor - _originY) + _pb;

        // ── 생성자 ─────────────────────────────────────────────────────────────
        /// <param name="panelX">패널 좌측 X (Screen 좌표).</param>
        /// <param name="panelY">패널 상단 Y (Screen 좌표).</param>
        /// <param name="panelWidth">패널 전체 너비.</param>
        /// <param name="padding">내부 여백 (EdgeInsets).</param>
        /// <param name="drawing">true = GUI 호출 활성, false = 높이 측정만.</param>
        public UIContext(float panelX, float panelY, float panelWidth,
                         EdgeInsets padding, bool drawing = true)
        {
            _drawing = drawing;
            _originY = panelY;
            _cx      = panelX     + padding.Left;
            _cw      = panelWidth - padding.Horizontal;
            _pb      = padding.Bottom;
            _cursor  = panelY     + padding.Top;
        }

        // ── 레이아웃 메서드 ────────────────────────────────────────────────────

        /// <summary>
        /// 레이블 행.
        /// <param name="height">레이블 rect 높이 — 폰트 크기보다 충분히 크게 설정.</param>
        /// <param name="gap">이 요소 아래 여백 (기본 2px).</param>
        /// </summary>
        public UIContext Label(string text, GUIStyle? style,
                               float height, float gap = 2f)
        {
            if (_drawing)
                GUI.Label(new Rect(_cx, _cursor, _cw, height), text, style!);
            _cursor += height + gap;
            return this;
        }

        /// <summary>
        /// GUI.color 착색 레이블.
        /// 호출 전후로 GUI.color 를 자동 저장·복원합니다.
        /// </summary>
        public UIContext Label(Color color, string text, GUIStyle? style,
                               float height, float gap = 2f)
        {
            if (_drawing)
            {
                var prev  = GUI.color;
                GUI.color = color;
                GUI.Label(new Rect(_cx, _cursor, _cw, height), text, style!);
                GUI.color = prev;
            }
            _cursor += height + gap;
            return this;
        }

        /// <summary>
        /// 가로 진행 바 (배경 + 채움 색상).
        /// </summary>
        public UIContext Bar(float ratio, Color fill, float alpha,
                             float height = 5f, float gap = 4f)
        {
            if (_drawing)
                OverlayStyle.DrawBar(
                    new Rect(_cx, _cursor, _cw, height), ratio, fill, alpha);
            _cursor += height + gap;
            return this;
        }

        /// <summary>빈 세로 여백 추가.</summary>
        public UIContext Space(float px)
        {
            _cursor += px;
            return this;
        }
    }
}
