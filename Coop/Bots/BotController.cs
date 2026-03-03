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

        private float _sectorScanAngle = 0f;
        private const float WanderRadius      = 5f;
        private const float ScanRotateSpeed   = 0.25f; // rad/s — ~25초 한 바퀴
        private const float AttackRange       = 4f;
        private const float ChaseRange        = 12f;
        private const float AttackCooldown    = 1.5f;
        private const float FormationDeadzone = 0.8f;  // m — 이 이내면 정지

        public BotController(int index, Vector3 spawnPos)
        {
            BotIndex = index;
            BotId    = BotState.BotSteamIds[index];
            Position = spawnPos;
            // 각 봇의 초기 각도를 등간격으로 분산 (사주경계 시작 포메이션)
            int total = Math.Max(1, BotState.Count);
            _sectorScanAngle = (float)(2.0 * Math.PI * index / total);
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
            {
                Position = centerPos;
                _sectorScanAngle = (float)(2.0 * Math.PI * BotIndex / Math.Max(1, BotState.Count));
            }
        }

        private void DoWander(float dt, Vector3 centerPos)
        {
            // 사주경계 포메이션: 봇 인덱스에 따른 등간격 방위각으로 플레이어 주위를 천천히 순찰
            _sectorScanAngle += ScanRotateSpeed * dt;

            float angle   = _sectorScanAngle;
            float targetX = centerPos.x + (float)Math.Cos(angle) * WanderRadius;
            float targetZ = centerPos.z + (float)Math.Sin(angle) * WanderRadius;

            float dx  = targetX - Position.x;
            float dz  = targetZ - Position.z;
            float len = (float)Math.Sqrt(dx*dx + dz*dz);
            DesiredMoveDir = len > FormationDeadzone
                ? new Vector3(dx / len, 0f, dz / len) : Vector3.zero;
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
