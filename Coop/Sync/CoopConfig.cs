namespace BloodshedModToolkit.Coop.Sync
{
    /// <summary>XP 공유 방식.</summary>
    public enum XpShareMode
    {
        /// <summary>각자 독립적으로 XP를 획득합니다 (동기화 없음).</summary>
        Independent = 0,

        /// <summary>Host XP를 Guest에게 그대로 복제합니다 (기본값).</summary>
        Replicate   = 1,

        /// <summary>Host XP를 절반으로 나눠 Guest에게 전달합니다.</summary>
        Split       = 2,
    }

    /// <summary>
    /// 런타임 Co-op 설정. 세션 도중 언제든지 변경 가능.
    /// </summary>
    public static class CoopConfig
    {
        /// <summary>현재 XP 공유 모드.</summary>
        public static XpShareMode XpShare { get; set; } = XpShareMode.Replicate;
    }
}
