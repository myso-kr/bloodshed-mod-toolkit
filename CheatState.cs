namespace BloodshedModToolkit
{
    public static class CheatState
    {
        public static bool GodMode;
        public static bool InfiniteGems;
        public static bool InfiniteSkullCoins;
        public static bool MaxStats;
        public static bool SpeedHack;
        public static bool OneShotKill;
        public static bool NoCooldown;
        public static bool InfiniteRevive;
        public static bool InfiniteAway;
        public static bool NoReload;
        public static bool RapidFire;
        public static bool NoRecoil;
        public static bool PerfectAim;

        public static float SpeedMultiplier = 1f;
        public static float GemsFloor       = 999999f;
        public static int   SkullCoinsFloor = 999;

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
