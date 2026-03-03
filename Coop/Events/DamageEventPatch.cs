using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Ecs;

namespace BloodshedModToolkit.Coop.Events
{
    // ── Host 측: 적 사망 감지 → EntityDespawn 브로드캐스트 ─────────────────
    [HarmonyPatch(typeof(Health), "Damage",
        new Type[]
        {
            typeof(float), typeof(GameObject),
            typeof(float), typeof(float),
            typeof(Vector3), typeof(Vector3), typeof(bool)
        })]
    public static class EnemyDeathEventPatch
    {
        static void Postfix(Health __instance)
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            if (__instance.isPlayer) return;

            if (__instance.currentHealth <= 0f)
            {
                int localId = __instance.GetInstanceID();
                EntityRegistry.LocalHealth.Remove(localId);
                EventBridge.OnEnemyDespawned((uint)localId);
            }
        }
    }

    // ── Guest 측: 적 피격 → DamageRequest를 Host에 전송 ────────────────────
    [HarmonyPatch(typeof(Health), "Damage",
        new Type[]
        {
            typeof(float), typeof(GameObject),
            typeof(float), typeof(float),
            typeof(Vector3), typeof(Vector3), typeof(bool)
        })]
    public static class GuestDamageRequestPatch
    {
        static void Prefix(Health __instance, float damage)
        {
            if (CoopState.IsHost || !CoopState.IsConnected) return;
            if (__instance.isPlayer) return;

            int localId = __instance.GetInstanceID();
            if (EntityRegistry.HostToLocal.TryGetHost(localId, out uint hostIdx))
                EventBridge.OnDamageRequest(hostIdx, damage);
        }
    }
}
