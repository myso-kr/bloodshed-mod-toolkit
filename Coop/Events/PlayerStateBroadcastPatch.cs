using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using Steamworks;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(Q3PlayerController), "Accelerate")]
    public static class PlayerStateBroadcastPatch
    {
        private static float _lastBroadcast;
        private const  float BroadcastInterval = 0.05f;  // 20 Hz

        /// <summary>봇이 Co-op 비활성 상태에서도 로컬 플레이어 위치를 참조할 수 있도록 항상 갱신.</summary>
        public static Vector3 LastKnownLocalPos;

        static void Postfix(Q3PlayerController __instance)
        {
            var pos = __instance.transform.position;
            LastKnownLocalPos = pos;              // ← 항상 갱신 (봇이 co-op 없이도 작동)

            if (!CoopState.IsConnected) return;
            float now = Time.time;
            if (now - _lastBroadcast < BroadcastInterval) return;
            _lastBroadcast = now;

            var ps = Object.FindObjectOfType<PlayerStats>();
            if (ps == null) return;

            EventBridge.OnPlayerStateChanged(
                steamId: (ulong)SteamUser.GetSteamID(),
                px: pos.x, py: pos.y, pz: pos.z,
                hp:    ps.CurrentHp,
                maxHp: ps.MaxHp,
                level: ps.level,
                xp:    ps.experience,
                xpCap: ps.experienceCap
            );
        }
    }
}
