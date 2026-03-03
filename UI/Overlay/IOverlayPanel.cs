namespace BloodshedModToolkit.UI.Overlay
{
    /// <summary>
    /// 우측 상단 오버레이에 추가할 수 있는 패널 계약.
    /// OverlayManager.Register() 로 등록하면 자동으로 수직 스택됩니다.
    ///
    /// 패널 구현 가이드:
    ///   1. Padding    : EdgeInsets.All(OverlayManager.Margin) 으로 표준 여백 사용
    ///   2. Height     : UIContext 측정 모드를 활용하거나 직접 계산
    ///   3. BackgroundAlpha : 고정값 또는 페이드 효과에 따른 동적 값
    ///   4. Draw(UIContext) : ctx.Label/Bar/Space 로 레이아웃 구성 (배경은 OverlayManager 담당)
    /// </summary>
    public interface IOverlayPanel
    {
        /// <summary>패널 식별자 (로그·디버그용).</summary>
        string Id { get; }

        /// <summary>false 면 공간도 차지하지 않고 완전히 숨김.</summary>
        bool Visible { get; }

        /// <summary>
        /// 이번 프레임의 패널 전체 높이 (px).
        /// UIContext 측정 모드로 자동 계산하거나 수식으로 직접 반환합니다.
        /// </summary>
        float Height { get; }

        /// <summary>
        /// 패널 내부 여백 — HTML padding 에 해당합니다.
        /// OverlayManager 가 UIContext 생성 시 사용합니다.
        /// </summary>
        EdgeInsets Padding { get; }

        /// <summary>
        /// 배경 반투명 알파 (0 ~ 1).
        /// OverlayManager 가 DrawBg 호출 시 사용합니다 (패널이 직접 배경을 그리지 않습니다).
        /// </summary>
        float BackgroundAlpha { get; }

        /// <summary>MonoBehaviour.Update 에서 매 프레임 호출 — 게임 로직 갱신.</summary>
        void Tick();

        /// <summary>
        /// IMGUI 렌더링.
        /// ctx 는 Padding 이 적용된 콘텐츠 영역 커서를 제공합니다.
        /// 배경은 OverlayManager 가 Draw 호출 전에 이미 그립니다.
        /// </summary>
        void Draw(UIContext ctx);
    }
}
