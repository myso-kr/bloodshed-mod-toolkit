using HarmonyLib;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// Host의 PlayerStats.SetMoney() 호출을 감지해 젬 델타를 Guest에게 브로드캐스트.
    /// Guest 측에서는 MoneySyncHandler.IsApplyingRemote 플래그로 재귀 방지.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "SetMoney")]
    public static class MoneyEventPatch
    {
        private static float _prevMoney;

        static void Prefix(PlayerStats __instance)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            _prevMoney = __instance.money;
        }

        static void Postfix(PlayerStats __instance)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            // 재귀 방지: 원격 적용 중이면 스킵 (Host는 기본적으로 원격 수신하지 않으나 안전장치)
            if (MoneySyncHandler.IsApplyingRemote) return;

            float delta = __instance.money - _prevMoney;
            if (System.Math.Abs(delta) < 0.001f) return;
            EventBridge.OnMoneyChanged(delta);
        }
    }
}
