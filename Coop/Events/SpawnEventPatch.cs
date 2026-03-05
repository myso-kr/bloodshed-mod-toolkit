using System;
using System.Collections.Generic;
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
            _prevEnemyCount = CountLiveEnemies();
        }

        static void Postfix()
        {
            if (!CoopState.IsConnected) return;

            var all = UnityEngine.Object.FindObjectsOfType<Health>();
            if (all == null) return;

            // 비-플레이어 Health 목록 추출
            var enemies = new List<Health>(all.Length);
            foreach (var h in all)
                if (!h.isPlayer) enemies.Add(h);

            int newCount = enemies.Count - _prevEnemyCount;
            if (newCount <= 0) return;

            if (CoopState.IsHost)
            {
                // Host: 신규 적을 Guest에게 브로드캐스트, LocalHealth 캐시에 즉시 등록
                for (int i = enemies.Count - newCount; i < enemies.Count; i++)
                {
                    var h    = enemies[i];
                    int id   = h.GetInstanceID();
                    EntityRegistry.LocalHealth[id] = h;

                    EventBridge.OnEnemySpawned(
                        hostEntityIdx: (uint)id,
                        typeId:        0,
                        x: 0f, y: 0f, z: 0f,
                        seed: (uint)UnityEngine.Random.Range(0, int.MaxValue));
                }
            }
            else
            {
                // Guest: PendingHostIds 큐에서 순서대로 매핑 + LocalHealth 캐시 등록
                for (int i = enemies.Count - newCount; i < enemies.Count; i++)
                {
                    if (!EntityRegistry.PendingHostIds.TryDequeue(out uint hostId)) break;

                    var h      = enemies[i];
                    int localId = h.GetInstanceID();
                    EntityRegistry.LocalHealth[localId] = h;
                    EntityRegistry.HostToLocal.Register(hostId, localId);

                    Plugin.Log.LogDebug(
                        $"[IDMapper] Host[{hostId}] ↔ Local[{localId}]");
                }
            }
        }

        private static int CountLiveEnemies()
        {
            var all = UnityEngine.Object.FindObjectsOfType<Health>();
            if (all == null) return 0;
            int count = 0;
            foreach (var h in all)
                if (!h.isPlayer) count++;
            return count;
        }
    }
}
