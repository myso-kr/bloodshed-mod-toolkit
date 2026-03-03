using System;
using UnityEngine;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Bots
{
    public enum BotAiState { Wander, Chase, Attack }

    /// <summary>순수 C# 이동 클래스 (MonoBehaviour 아님). 스레드 안전한 System.Random 사용.</summary>
    public class BotController
    {
        public ulong   BotId;
        public int     BotIndex;
        public Vector3 Position;
        public float   CurrentHp = 100f, MaxHp = 100f;
        public int     Level = 1;
        public float   Experience = 0f, ExperienceCap = 100f;

        public BotAiState AiState        = BotAiState.Wander;
        public Vector3    DesiredMoveDir = Vector3.zero;
        public bool       ShouldAttack  = false;
        private float     _attackCooldown = 0f;

        private Vector3 _targetPos;
        private float   _wanderTimer;
        private const float WanderRadius   = 8f;
        private const float WanderInterval = 3f;
        private const float AttackRange    = 4f;
        private const float ChaseRange     = 12f;
        private const float AttackCooldown = 1.5f;

        private static readonly System.Random s_rng = new();

        public BotController(int index, Vector3 spawnPos)
        {
            BotIndex   = index;
            BotId      = BotState.BotSteamIds[index];
            Position   = spawnPos;
            _targetPos = spawnPos;
            _wanderTimer = WanderInterval; // 즉시 첫 목표 선정
        }

        public void Tick(float dt, Vector3 centerPos, Vector3 enemyPos, float enemyDist)
        {
            ShouldAttack = false;
            if (_attackCooldown > 0f) _attackCooldown -= dt;

            // 상태 전환
            if      (enemyDist <= AttackRange) AiState = BotAiState.Attack;
            else if (enemyDist <= ChaseRange)  AiState = BotAiState.Chase;
            else                               AiState = BotAiState.Wander;

            switch (AiState)
            {
                case BotAiState.Wander: DoWander(dt, centerPos); break;
                case BotAiState.Chase:  DoChase(enemyPos);       break;
                case BotAiState.Attack: DoAttack();               break;
            }

            // 플레이어에서 3× 반경 이탈 시 리셋
            float dx = Position.x - centerPos.x, dz = Position.z - centerPos.z;
            if (dx*dx + dz*dz > (WanderRadius*3f)*(WanderRadius*3f))
                Position = _targetPos = centerPos;
        }

        private void DoWander(float dt, Vector3 centerPos)
        {
            _wanderTimer += dt;
            if (_wanderTimer >= WanderInterval)
            {
                _wanderTimer = 0f;
                float angle  = (float)(s_rng.NextDouble() * Math.PI * 2.0);
                float radius = (float)(s_rng.NextDouble() * WanderRadius);
                _targetPos = new Vector3(
                    centerPos.x + (float)Math.Cos(angle) * radius,
                    centerPos.y,
                    centerPos.z + (float)Math.Sin(angle) * radius);
            }
            var diff  = new Vector3(_targetPos.x - Position.x, 0f, _targetPos.z - Position.z);
            float len = (float)Math.Sqrt(diff.x*diff.x + diff.z*diff.z);
            DesiredMoveDir = len > 0.01f
                ? new Vector3(diff.x / len, 0f, diff.z / len) : Vector3.zero;
        }

        private void DoChase(Vector3 enemyPos)
        {
            var diff  = new Vector3(enemyPos.x - Position.x, 0f, enemyPos.z - Position.z);
            float len = (float)Math.Sqrt(diff.x*diff.x + diff.z*diff.z);
            DesiredMoveDir = len > 0.01f
                ? new Vector3(diff.x / len, 0f, diff.z / len) : Vector3.zero;
        }

        private void DoAttack()
        {
            DesiredMoveDir = Vector3.zero;
            if (_attackCooldown <= 0f) { ShouldAttack = true; _attackCooldown = AttackCooldown; }
        }

        public PlayerStatePacket ToPacket() => new()
        {
            SteamId       = BotId,
            PosX          = Position.x,
            PosY          = Position.y,
            PosZ          = Position.z,
            CurrentHp     = CurrentHp,
            MaxHp         = MaxHp,
            Level         = Level,
            Experience    = Experience,
            ExperienceCap = ExperienceCap,
        };
    }
}
