namespace BloodshedModToolkit.Tweaks
{
    public enum TweakPresetType { Mortal, Hunter, Slayer, Demon, Apocalypse }

    public static class TweakPresets
    {
        public static TweakConfig Get(TweakPresetType preset) => preset switch
        {
            TweakPresetType.Mortal     => Mortal,
            TweakPresetType.Slayer     => Slayer,
            TweakPresetType.Demon      => Demon,
            TweakPresetType.Apocalypse => Apocalypse,
            _                          => Hunter,
        };

        // ── Mortal — 쉬움: 플레이어에게 유리 ───────────────────────────────────────
        public static TweakConfig Mortal => new TweakConfig
        {
            PlayerHpMult          = 2.0f,   // 체력 2배
            PlayerSpeedMult       = 1.2f,   // 이동속도 +20%
            WeaponDamageMult      = 1.5f,   // 무기 데미지 +50%
            WeaponFireRateMult    = 1.3f,   // 발사속도 +30%
            WeaponReloadSpeedMult = 1.5f,   // 장전속도 +50%
            EnemyHpMult           = 0.7f,   // 에너미 체력 -30%
            EnemySpeedMult        = 0.8f,   // 에너미 이동속도 -20%
            EnemyDamageMult       = 0.6f,   // 에너미 → 플레이어 데미지 -40%
            SpawnCountMult        = 0.7f,   // 스폰 수 -30%
        };

        // ── Hunter — 기본: 게임 기본값 그대로 ────────────────────────────────────
        public static TweakConfig Hunter => new TweakConfig();

        // ── Slayer — 어려움 ───────────────────────────────────────────────────────
        public static TweakConfig Slayer => new TweakConfig
        {
            PlayerHpMult    = 0.75f,  // 체력 -25%
            EnemyHpMult     = 1.5f,   // 에너미 체력 +50%
            EnemySpeedMult  = 1.25f,  // 에너미 이동속도 +25%
            EnemyDamageMult = 1.5f,   // 에너미 → 플레이어 데미지 +50%
            SpawnCountMult  = 1.5f,   // 스폰 수 +50%
        };

        // ── Demon — 매우 어려움 ───────────────────────────────────────────────────
        public static TweakConfig Demon => new TweakConfig
        {
            PlayerHpMult      = 0.5f,   // 체력 -50%
            WeaponDamageMult  = 0.9f,   // 무기 데미지 -10%
            EnemyHpMult       = 2.5f,   // 에너미 체력 +150%
            EnemySpeedMult    = 1.5f,   // 에너미 이동속도 +50%
            EnemyDamageMult   = 2.0f,   // 에너미 → 플레이어 데미지 +100%
            SpawnCountMult    = 2.0f,   // 스폰 수 +100%
        };

        // ── Apocalypse — 극한 ─────────────────────────────────────────────────────
        public static TweakConfig Apocalypse => new TweakConfig
        {
            PlayerHpMult      = 0.25f,  // 체력 -75%
            WeaponDamageMult  = 0.75f,  // 무기 데미지 -25%
            EnemyHpMult       = 4.0f,   // 에너미 체력 +300%
            EnemySpeedMult    = 2.0f,   // 에너미 이동속도 +100%
            EnemyDamageMult   = 3.0f,   // 에너미 → 플레이어 데미지 +200%
            SpawnCountMult    = 3.0f,   // 스폰 수 +200%
        };
    }
}
