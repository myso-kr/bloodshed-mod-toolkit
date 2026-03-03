namespace BloodshedModToolkit
{
    public static class CheatState
    {
        public static bool GodMode            { get; set; }
        public static bool InfiniteGems       { get; set; }
        public static bool InfiniteSkullCoins { get; set; }
        public static bool MaxStats           { get; set; }
        public static bool SpeedHack          { get; set; }
        public static bool OneShotKill        { get; set; }
        public static bool NoCooldown         { get; set; }
        public static bool InfiniteRevive     { get; set; }
        public static bool InfiniteAway       { get; set; }
        public static bool NoReload          { get; set; }
        public static bool RapidFire        { get; set; }
        public static bool NoRecoil         { get; set; }
        public static bool PerfectAim       { get; set; }

        public static float SpeedMultiplier { get; set; } = 1f;   // 기본 1× (배속 없음)
        public static float GemsFloor       { get; set; } = 999999f;
        public static int   SkullCoinsFloor { get; set; } = 999;

        public static void Initialize()
        {
            GodMode            = false;
            InfiniteGems       = false;
            InfiniteSkullCoins = false;
            MaxStats           = false;
            SpeedHack          = false;
            OneShotKill        = false;
            NoCooldown         = false;
            InfiniteRevive     = false;
            InfiniteAway       = false;
            NoReload           = false;
            RapidFire          = false;
            NoRecoil           = false;
            PerfectAim         = false;

            SpeedMultiplier   = 1f;
            GemsFloor         = 999999f;
            SkullCoinsFloor   = 999;
        }
    }
}
