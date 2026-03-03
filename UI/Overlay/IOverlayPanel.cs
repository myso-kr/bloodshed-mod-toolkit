using UnityEngine;

namespace BloodshedModToolkit.UI.Overlay
{
    /// <summary>
    /// 우측 상단 오버레이에 추가할 수 있는 패널 계약.
    /// OverlayManager.Register() 로 등록하면 자동으로 수직 스택됩니다.
    /// </summary>
    public interface IOverlayPanel
    {
        /// <summary>패널 식별자 (로그/디버그용).</summary>
        string Id { get; }

        /// <summary>false 면 공간도 차지하지 않고 완전히 숨김.</summary>
        bool Visible { get; }

        /// <summary>
        /// 이번 프레임의 패널 높이 (px).
        /// OverlayManager 가 Draw 직전에 읽어 위치를 계산합니다.
        /// </summary>
        float Height { get; }

        /// <summary>MonoBehaviour.Update 에서 매 프레임 호출 — 게임 로직 갱신.</summary>
        void Tick();

        /// <summary>
        /// IMGUI 렌더링. rect 는 배경 포함 전체 영역입니다.
        /// 배경 박스 및 내부 콘텐츠 모두 이 메서드에서 직접 그립니다.
        /// </summary>
        void Draw(Rect rect);
    }
}
