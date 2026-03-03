using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(SpawnDirector), "SpawnEnemies",
        new Type[] { typeof(Transform), typeof(int), typeof(bool) })]
    public static class SpawnEventPatch
    {
        private static int _prevEnemyCount;

        static void Prefix()
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            var all = UnityEngine.Object.FindObjectsOfType<EnemyIdentityCard>();
            _prevEnemyCount = all?.Length ?? 0;
        }

        static void Postfix()
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            var all = UnityEngine.Object.FindObjectsOfType<EnemyIdentityCard>();
            if (all == null) return;

            int newCount = all.Length - _prevEnemyCount;
            if (newCount <= 0) return;

            for (int i = all.Length - newCount; i < all.Length; i++)
            {
                var card = all[i];
                // TODO Phase 4: 실제 위치는 ECS Entity 매핑으로 정밀화 예정
                EventBridge.OnEnemySpawned(
                    hostEntityIdx: (uint)card.GetInstanceID(),
                    typeId:        0,
                    x: 0f, y: 0f, z: 0f,
                    seed: (uint)UnityEngine.Random.Range(0, int.MaxValue)
                );
            }
        }
    }
}
