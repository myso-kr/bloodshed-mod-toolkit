using System.Collections.Generic;
using UnityEngine;

namespace BloodshedModToolkit.UI.Overlay
{
    public enum OverlayPosition { Hidden, TopLeft, TopCenter, TopRight }

    /// <summary>
    /// 우측 상단 오버레이 패널을 관리합니다.
    ///
    /// 사용법:
    ///   1. CheatMenu.Awake()   에서 OverlayManager.Register(new MyPanel()) 호출
    ///   2. CheatMenu.Update()  에서 OverlayManager.Tick() 호출
    ///   3. CheatMenu.OnGUI()   에서 OverlayManager.Draw() 호출
    ///
    /// 패널은 등록 순서대로 위→아래로 쌓입니다.
    /// 배경은 OverlayManager 가 p.Height / p.BackgroundAlpha 를 읽어 직접 그립니다.
    /// 콘텐츠는 UIContext 를 통해 p.Draw(ctx) 로 렌더링합니다.
    /// </summary>
    public static class OverlayManager
    {
        // ── 레이아웃 상수 ─────────────────────────────────────────────────────
        public const float PanelWidth = 210f;   // 패널 너비
        public const float Margin     = 8f;     // 화면 가장자리 여백 / 기본 패딩
        public const float LineH      = 16f;    // 기본 줄 높이 (StatusPanel 등에서 사용)
        public const float Gap        = 4f;     // 패널 간 간격

        // ── 상태 ─────────────────────────────────────────────────────────────
        /// <summary>CheatMenu 의 열림/닫힘 상태 — StatusPanel 이 화살표 표시에 사용.</summary>
        public static bool IsMenuOpen { get; set; }

        /// <summary>고정 UI 위치. Hidden 으로 설정하면 오버레이 전체를 숨깁니다.</summary>
        public static OverlayPosition Position { get; set; } = OverlayPosition.TopRight;

        private static readonly List<IOverlayPanel> _panels = new();

        // ── 패널 등록 ─────────────────────────────────────────────────────────
        public static void Register(IOverlayPanel panel) => _panels.Add(panel);

        // ── Tick (Update에서 호출) ────────────────────────────────────────────
        public static void Tick()
        {
            foreach (var p in _panels)
                p.Tick();
        }

        // ── Draw (OnGUI에서 호출) ─────────────────────────────────────────────
        /// <summary>
        /// 모든 가시 패널을 수직으로 스택하여 그립니다.
        /// 오버레이 영역을 클릭했으면 true 를 반환합니다 (메뉴 토글 트리거).
        /// </summary>
        public static bool Draw()
        {
            OverlayStyle.EnsureReady();

            if (Position == OverlayPosition.Hidden) return false;

            float px = Position switch
            {
                OverlayPosition.TopLeft   => Margin,
                OverlayPosition.TopCenter => (Screen.width - PanelWidth) * 0.5f,
                _                         => Screen.width - PanelWidth - Margin,
            };
            float py      = Margin;
            bool  clicked = false;

            foreach (var p in _panels)
            {
                if (!p.Visible) continue;

                float h        = p.Height;
                var   panelRect = new Rect(px, py, PanelWidth, h);

                // 배경 — 패널이 선언한 알파로 그림 (패널은 직접 DrawBg 를 호출하지 않음)
                OverlayStyle.DrawBg(panelRect, p.BackgroundAlpha);

                // 콘텐츠 — UIContext 를 통해 패딩 + 자동 커서 이동
                var ctx = new UIContext(px, py, PanelWidth, p.Padding);
                p.Draw(ctx);

                // 투명 클릭 영역 (콘텐츠가 이벤트를 소비하지 않은 경우에만 발동)
                if (GUI.Button(panelRect, GUIContent.none, GUIStyle.none))
                    clicked = true;

                py += h + Gap;
            }

            return clicked;
        }
    }
}
