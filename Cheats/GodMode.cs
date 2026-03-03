using HarmonyLib;
using com8com1.SCFPS;
using UnityEngine;

namespace BloodshedModToolkit.Cheats
{
    /// <summary>
    /// PlayerStats.TakeDamage 를 Prefix 로 후킹해 플레이어 피해를 차단합니다.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStats), "TakeDamage")]
    public static class GodModePatch
    {
        // damage: Single, instigator: GameObject
        static bool Prefix(float damage, GameObject instigator)
        {
            if (CheatState.GodMode)
            {
                Plugin.Log.LogDebug($"[GodMode] TakeDamage 차단 — damage={damage}");
                return false; // 원본 메서드 스킵 → 피해 없음
            }
            return true;
        }
    }
}
