using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
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
                EventBridge.OnEnemyDespawned((uint)__instance.GetInstanceID());
        }
    }
}
