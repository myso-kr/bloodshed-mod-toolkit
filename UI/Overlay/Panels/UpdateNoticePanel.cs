using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.I18n;

namespace BloodshedModToolkit.UI.Overlay.Panels
{
    /// <summary>
    /// GitHub에서 더 새로운 버전이 감지되면 우측 상단에 표시하는 업데이트 알림 패널.
    /// [✕] 버튼으로 세션 내 숨김 처리 가능 (재시작 시 초기화).
    /// </summary>
    public sealed class UpdateNoticePanel : IOverlayPanel
    {
        private bool _dismissed;

        public string     Id              => "update_notice";
        public bool       Visible         => UpdateChecker.IsOutdated && !_dismissed;
        public EdgeInsets Padding         => EdgeInsets.All(OverlayManager.Margin);
        public float      BackgroundAlpha => 0.85f;

        // 2줄 (알림 + URL) + 상하 패딩
        public float Height =>
            Padding.Vertical + 2f * OverlayManager.LineH;

        // ── 언어 조회 ───────────────────────────────────────────────────────────
        private GameSettings? _gs;

        private GameSettings? GS()
        {
            if (_gs != null && _gs.isActiveAndEnabled) return _gs;
            return _gs = UnityEngine.Object.FindObjectOfType<GameSettings>();
        }

        private LangStrings Lang()
            => Strings.Get(GS()?.languageText ?? LocalizationManager.Language.English);

        // ── IOverlayPanel ───────────────────────────────────────────────────────
        public void Tick() { }

        public void Draw(UIContext ctx)
        {
            if (_dismissed) return;

            var    l       = Lang();
            float  lh      = OverlayManager.LineH;
            string current = MyPluginInfo.PLUGIN_VERSION;
            string latest  = UpdateChecker.LatestVersion ?? current;

            float dismissW = 20f;
            float labelW   = ctx.ContentW - dismissW - 4f;

            // 첫째 줄 Y를 레이블 그리기 전에 캡처
            float rowY  = ctx.CursorY;
            string notice = string.Format(l.UpdateAvailable, current, latest);
            ctx.Label(OverlayStyle.Amber, notice, OverlayStyle.Item!, lh, gap: 0f);

            // [✕] 버튼 — 첫째 줄 오른쪽 끝에 오버랩
            if (GUI.Button(new Rect(ctx.ContentX + labelW + 4f, rowY, dismissW, lh), new GUIContent("✕"), OverlayStyle.Item!))
                _dismissed = true;

            // 둘째 줄: URL (dim 색상)
            ctx.Label(OverlayStyle.Dim,
                      "github.com/myso-kr/bloodshed-mod-toolkit/releases",
                      OverlayStyle.Item!, lh, gap: 0f);
        }
    }
}
