namespace BloodshedModToolkit.Tweaks
{
    public static class TweakState
    {
        public static TweakPresetType ActivePreset { get; private set; } = TweakPresetType.Hunter;
        public static TweakConfig Current          { get; private set; } = new TweakConfig();

        public static void Apply(TweakPresetType preset)
        {
            ActivePreset = preset;
            Current      = TweakPresets.Get(preset);
            Plugin.Log.LogInfo($"[TweakState] 프리셋 → {preset}");
        }

        public static void Initialize() => Apply(TweakPresetType.Hunter);
    }
}
