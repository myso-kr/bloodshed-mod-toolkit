using HarmonyLib;
using UnityEngine;
using Steamworks;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    /// <summary>
    /// PlayerStats.RecalculateStats() 완료 후 최신 스탯을 상대방에게 브로드캐스트.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "RecalculateStats")]
    public static class RecalcStatsPatch
    {
        static void Postfix(PlayerStats __instance)
        {
            if (!CoopState.IsConnected) return;

            float px = 0f, py = 0f, pz = 0f;
            var tr = __instance.transform;
            if (tr != null)
            {
                px = tr.position.x;
                py = tr.position.y;
                pz = tr.position.z;
            }

            EventBridge.OnPlayerStateChanged(
                (ulong)SteamUser.GetSteamID(),
                px, py, pz,
                __instance.CurrentHp, __instance.MaxHp,
                __instance.level,
                __instance.experience, __instance.experienceCap);
        }
    }
}
