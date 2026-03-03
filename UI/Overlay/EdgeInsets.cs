namespace BloodshedModToolkit.UI.Overlay
{
    /// <summary>
    /// HTML padding / margin 에 해당하는 상하좌우 간격 값 (read-only struct).
    /// </summary>
    public readonly struct EdgeInsets
    {
        public readonly float Top;
        public readonly float Right;
        public readonly float Bottom;
        public readonly float Left;

        public EdgeInsets(float top, float right, float bottom, float left)
        {
            Top = top; Right = right; Bottom = bottom; Left = left;
        }

        /// <summary>수평 합계 (Left + Right).</summary>
        public float Horizontal => Left + Right;

        /// <summary>수직 합계 (Top + Bottom).</summary>
        public float Vertical   => Top  + Bottom;

        /// <summary>네 변을 동일한 값으로 설정.</summary>
        public static EdgeInsets All(float v)
            => new EdgeInsets(v, v, v, v);

        /// <summary>수직(상하) / 수평(좌우) 을 각각 지정.</summary>
        public static EdgeInsets Symmetric(float vertical, float horizontal)
            => new EdgeInsets(vertical, horizontal, vertical, horizontal);
    }
}
