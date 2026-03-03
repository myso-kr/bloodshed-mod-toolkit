using System.Collections.Generic;
using UnityEngine;

namespace BloodshedModToolkit.UI.Overlay
{
    /// <summary>
    /// 우측 상단 오버레이 패널을 관리합니다.
    ///
    /// 사용법:
    ///   1. Plugin 또는 CheatMenu.Awake() 에서 OverlayManager.Register(new MyPanel()) 호출
    ///   2. CheatMenu.Update()  에서 OverlayManager.Tick() 호출
    ///   3. CheatMenu.OnGUI()   에서 OverlayManager.Draw() 호출
    ///
    /// 패널은 등록 순서대로 위→아래로 쌓입니다.
    /// </summary>
    public static class OverlayManager
    {
        // ── 레이아웃 상수 ─────────────────────────────────────────────────────
        public const float Width  = 210f;   // 패널 너비
        public const float Margin = 8f;     // 내부 여백
        public const float LineH  = 16f;    // 기본 줄 높이
        public const float Gap    = 4f;     // 패널 간 간격

        // ── 상태 ─────────────────────────────────────────────────────────────
        /// <summary>CheatMenu 의 열림/닫힘 상태 — StatusPanel 이 화살표 표시에 사용.</summary>
        public static bool IsMenuOpen { get; set; }

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

            float px      = Screen.width - Width - Margin;
            float py      = Margin;
            bool  clicked = false;

            foreach (var p in _panels)
            {
                if (!p.Visible) continue;

                float h    = p.Height;
                var   rect = new Rect(px, py, Width, h);

                // 패널 콘텐츠 먼저 — 내부 버튼이 있으면 이벤트를 소비하도록
                p.Draw(rect);

                // 투명 클릭 영역: 콘텐츠가 이벤트를 소비하지 않은 경우에만 발동
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    clicked = true;

                py += h + Gap;
            }

            return clicked;
        }
    }
}
