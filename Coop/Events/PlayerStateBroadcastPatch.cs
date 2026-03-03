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

        static void Postfix(Q3PlayerController __instance)
        {
            if (!CoopState.IsConnected) return;
            float now = Time.time;
            if (now - _lastBroadcast < BroadcastInterval) return;
            _lastBroadcast = now;

            var ps = Object.FindObjectOfType<PlayerStats>();
            if (ps == null) return;

            var pos = __instance.transform.position;
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
