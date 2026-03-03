using System;
using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop.Events;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Renderer;

namespace BloodshedModToolkit.Coop.Bots
{
    public class BotManager : MonoBehaviour
    {
        public BotManager(IntPtr ptr) : base(ptr) { }
        public static BotManager? Instance { get; private set; }

        private readonly List<BotController> _bots = new();
        private const float TickInterval = 0.05f;  // 20 Hz
        private float _tickTimer;

        void Awake() { Instance = this; Plugin.Log.LogInfo("[BotManager] loaded"); }
        void OnDestroy() { RemoveAll(); Instance = null; }

        void Update()
        {
            if (!BotState.Enabled) { if (_bots.Count > 0) RemoveAll(); return; }

            int desired = Math.Clamp(BotState.Count, 1, 3);
            Vector3 localPos = PlayerStateBroadcastPatch.LastKnownLocalPos;

            // 로컬 플레이어 위치가 아직 0이면 스폰 보류 (메인메뉴 대비)
            if (localPos.x == 0f && localPos.y == 0f && localPos.z == 0f) return;

            SyncCount(desired, localPos);

            _tickTimer += Time.deltaTime;
            if (_tickTimer < TickInterval) return;
            _tickTimer -= TickInterval;

            foreach (var bot in _bots)
            {
                BotPhysicsBody.Instances.TryGetValue(bot.BotId, out var pb);

                if (pb != null) bot.Position = pb.position;

                float    enemyDist   = pb?.NearestEnemyDist ?? float.MaxValue;
                Vector3  enemyPos    = pb?.NearestEnemyPos  ?? Vector3.zero;
                var      enemyHealth = pb?.NearestEnemy;

                bot.Tick(TickInterval, localPos, enemyPos, enemyDist);

                if (pb != null) pb.SetMoveDir(bot.DesiredMoveDir);

                if (bot.ShouldAttack && enemyHealth != null)
                {
                    enemyHealth.Damage(25f, null!, 0.1f, 0f,
                        bot.DesiredMoveDir, enemyPos, false);
                    Plugin.Log.LogInfo(
                        $"[Bot] {BotState.BotNames[bot.BotIndex]} → 적 25 dmg");
                    bot.ShouldAttack = false;

                    // ── 공격 모션 ──
                    if (BotAvatarAnimator.Instances.TryGetValue(bot.BotId, out var anim))
                        anim.TriggerAttack();
                }

                PlayerSyncHandler.OnPlayerState(bot.BotId, bot.ToPacket());
            }
        }

        private void SyncCount(int desired, Vector3 localPos)
        {
            while (_bots.Count < desired)
            {
                int idx = _bots.Count;
                // Y + 1.5f: CharacterController(center=0)는 중력으로 바닥까지 낙하 → 지면 착지 보장
                var spawn = new Vector3(localPos.x + (idx - 1) * 2f, localPos.y + 1.5f, localPos.z);
                _bots.Add(new BotController(idx, spawn));
                Plugin.Log.LogInfo($"[BotManager] 봇 추가: {BotState.BotNames[idx]}");
            }
            while (_bots.Count > desired)
            {
                var removed = _bots[_bots.Count - 1];
                _bots.RemoveAt(_bots.Count - 1);
                PlayerSyncHandler.RemoveBot(removed.BotId);
                Plugin.Log.LogInfo($"[BotManager] 봇 제거: {BotState.BotNames[removed.BotIndex]}");
            }
        }

        private void RemoveAll()
        {
            foreach (var bot in _bots) PlayerSyncHandler.RemoveBot(bot.BotId);
            _bots.Clear();
        }

        public IReadOnlyList<BotController> GetBots() => _bots;
    }
}
