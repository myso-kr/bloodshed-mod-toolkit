using System;
using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(SpawnProcessor), "NextWave")]
    public static class WaveAdvancePatch
    {
        static void Postfix(SpawnProcessor __instance)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            EventBridge.OnWaveAdvanced(
                waveIdx: __instance.currentWaveIndex,
                seed:    (uint)(DateTime.UtcNow.Ticks & 0xFFFFFFFF)
            );
        }
    }

    [HarmonyPatch(typeof(SpawnProcessor), "StartNewWaveGroup")]
    public static class WaveGroupStartPatch
    {
        // HandleWaveAdvance에서 Guest가 호출 시 일시적으로 true로 설정
        internal static bool _allowGuestTrigger = false;

        static bool Prefix()
        {
            if (!CoopState.IsHost && CoopState.IsConnected && !_allowGuestTrigger)
                return false;
            return true;
        }
    }
}
