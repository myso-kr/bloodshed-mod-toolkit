using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(PlayerStats), "AddXp")]
    public static class XpEventPatch
    {
        // NetManager.HandleXpGained에서 설정 — 원격 수신으로 인한 재귀 브로드캐스트 방지
        internal static bool _applyingRemoteXp = false;

        static void Postfix(float amount)
        {
            if (_applyingRemoteXp) return;
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            EventBridge.OnXpGained(amount);
        }
    }
}
