using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using Steamworks;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// ShotAction.GetSpreadDirection Postfix — 로컬 플레이어가 공격할 때 AttackEvent 브로드캐스트.
    /// 피어 아바타의 공격 애니메이션을 동기화하는 데 사용됩니다.
    /// </summary>
    [HarmonyPatch(typeof(ShotAction), "GetSpreadDirection")]
    public static class AttackEventPatch
    {
        private static float _lastAttack;
        private const  float AttackCooldown = 0.05f;  // 50ms — 연사 시 과다 발송 방지

        static void Postfix(ShotAction __instance, Vector3 direction)
        {
            if (!CoopState.IsConnected) return;
            float now = Time.time;
            if (now - _lastAttack < AttackCooldown) return;

            // 로컬 플레이어의 ShotAction인지 확인 (PlayerStats 부모 컴포넌트 유무)
            var ps = __instance.GetComponentInParent<PlayerStats>();
            if (ps == null) return;

            _lastAttack = now;
            EventBridge.OnAttack((ulong)SteamUser.GetSteamID());
        }
    }
}
