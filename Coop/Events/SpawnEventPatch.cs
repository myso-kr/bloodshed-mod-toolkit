using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Ecs;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(SpawnDirector), "SpawnEnemies",
        new Type[] { typeof(Transform), typeof(int), typeof(bool) })]
    public static class SpawnEventPatch
    {
        private static int _prevEnemyCount;

        static void Prefix()
        {
            if (!CoopState.IsConnected) return;
            var all = UnityEngine.Object.FindObjectsOfType<EnemyIdentityCard>();
            _prevEnemyCount = all?.Length ?? 0;
        }

        static void Postfix()
        {
            if (!CoopState.IsConnected) return;
            var all = UnityEngine.Object.FindObjectsOfType<EnemyIdentityCard>();
            if (all == null) return;

            int newCount = all.Length - _prevEnemyCount;
            if (newCount <= 0) return;

            if (CoopState.IsHost)
            {
                // Host: 신규 스폰을 Guest에게 브로드캐스트
                // Health 캐시는 StateApplicator / HandleDamageRequest 에서 lazy-init 처리
                for (int i = all.Length - newCount; i < all.Length; i++)
                {
                    var card    = all[i];
                    int localId = card.GetInstanceID();

                    EventBridge.OnEnemySpawned(
                        hostEntityIdx: (uint)localId,
                        typeId:        0,
                        x: 0f, y: 0f, z: 0f,
                        seed: (uint)UnityEngine.Random.Range(0, int.MaxValue));
                }
            }
            else
            {
                // Guest: PendingHostIds 큐에서 순서대로 매핑
                // Health 캐시는 StateApplicator.ApplyEntry 에서 lazy-init 처리
                for (int i = all.Length - newCount; i < all.Length; i++)
                {
                    if (!EntityRegistry.PendingHostIds.TryDequeue(out uint hostId))
                        break;

                    int localId = all[i].GetInstanceID();
                    EntityRegistry.HostToLocal.Register(hostId, localId);

                    Plugin.Log.LogDebug(
                        $"[IDMapper] Host[{hostId}] ↔ Local[{localId}]");
                }
            }
        }
    }
}
