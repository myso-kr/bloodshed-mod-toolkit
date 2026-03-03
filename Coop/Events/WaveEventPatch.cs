using System;
using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Mission;

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
            // Guest가 연결 중일 때는 Host 명령 없이 자체 실행 차단
            if (!CoopState.IsHost && CoopState.IsConnected && !_allowGuestTrigger)
            {
                // Permitted 상태(Guest Ready 완료)일 때만 허용
                // Idle 포함 나머지 상태(WaitingForHost, ReadyUp)는 차단
                if (MissionState.Status == MissionStatus.Permitted)
                    return true;
                return false;
            }
            return true;
        }
    }
}
