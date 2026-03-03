using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;

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

            // 프리셋 전환 즉시 반영 — 게임 미시작 시에는 null이므로 안전
            Object.FindObjectOfType<PlayerStats>()?.RecalculateStats();
            var enemies = Object.FindObjectsOfType<EnemyAbilityController>();
            if (enemies != null)
                foreach (var ec in enemies) ec.RefreshAgentSpeed();

            // Phase 7: Co-op Host → Guest TweakConfig 동기화
            Coop.Sync.TweakSyncHandler.OnPresetApplied();
        }

        public static void Initialize() => Apply(TweakPresetType.Hunter);
    }
}
