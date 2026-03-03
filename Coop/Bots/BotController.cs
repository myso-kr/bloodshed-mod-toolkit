using System;
using UnityEngine;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Bots
{
    public enum BotAiState { Wander, Chase, Attack }

    /// <summary>순수 C# AI 로직 (MonoBehaviour 아님). 장애물 회피는 BotPhysicsBody Whisker에 위임.</summary>
    public class BotController
    {
        public ulong   BotId;
        public int     BotIndex;
        public Vector3 Position;
        public float   CurrentHp = 100f, MaxHp = 100f;
        public int     Level = 1;
        public float   Experience = 0f, ExperienceCap = 100f;

        public WeaponClass WeaponClass   = WeaponClass.Melee;
        public BotAiState  AiState       = BotAiState.Wander;
        public Vector3     DesiredMoveDir = Vector3.zero;
        public bool        ShouldAttack  = false;
        private float      _attackCooldown = 0f;

        // ── Reynolds Wander 상태 ──────────────────────────────────────────────
        private static readonly System.Random _rng = new();
        private float   _wanderAngle = 0f;  // 윈더 원 위의 현재 각도 (rad)
        private float   _heading     = 0f;  // 현재 진행 방위각 (atan2)
        private Vector3 _lastPos;           // 막힘 감지용 이전 위치
        private float   _stuckTimer  = 0f;
        private bool    _isInAttack  = false; // 상태 히스테리시스 플래그

        private const float WanderJitter       = 1.8f; // rad/s — 각도 지터량
        private const float WanderCircleDist   = 2.5f; // 전방 원 중심 투영 거리 (m)
        private const float WanderCircleRadius = 1.5f; // 윈더 원 반경 (m)
        private const float WanderLeashRadius  = 7f;   // 플레이어 이탈 한계 (m)
        private const float FormationDeadzone  = 0.8f; // 이 이내면 정지 (m)

        // ── WeaponClass별 전투 파라미터 ───────────────────────────────────────
        private float AttackRange => WeaponClass switch {
            WeaponClass.Melee    => 2.0f,
            WeaponClass.Pistol   => 8.0f,
            WeaponClass.Rifle    => 15.0f,
            WeaponClass.Launcher => 20.0f,
            _                    => 4.0f,
        };
        // 감지 반경: 공격 사거리 × 2.5 + 4m, 상한 22m
        // Melee≈9m / Pistol≈22m / Rifle·Launcher=22m (비례 상한)
        private float ChaseRange => Math.Min(AttackRange * 2.5f + 4f, 22f);
        private float AttackCooldownVal => WeaponClass switch {
            WeaponClass.Melee    => 0.8f,
            WeaponClass.Pistol   => 1.2f,
            WeaponClass.Rifle    => 2.0f,
            WeaponClass.Launcher => 3.5f,
            _                    => 1.5f,
        };

        public BotController(int index, Vector3 spawnPos)
        {
            BotIndex = index;
            BotId    = BotState.BotSteamIds[index];
            Position = spawnPos;
            _lastPos = spawnPos;
            // 봇별 등간격 초기 방향 — 사주경계 분산
            int total = Math.Max(1, BotState.Count);
            _heading     = (float)(2.0 * Math.PI * index / total);
            _wanderAngle = _heading;
        }

        public void Tick(float dt, Vector3 centerPos, Vector3 enemyPos, float enemyDist)
        {
            ShouldAttack = false;
            if (_attackCooldown > 0f) _attackCooldown -= dt;

            // ── 상태 전환 (히스테리시스 밴드) ─────────────────────────────
            // AttackRange 진입 → Attack, 이탈 후에도 ×1.6 이내면 Attack 유지
            if (enemyDist <= AttackRange)
            {
                AiState = BotAiState.Attack;
                _isInAttack = true;
            }
            else if (_isInAttack && enemyDist <= AttackRange * 1.6f)
            {
                AiState = BotAiState.Attack; // 히스테리시스 존
            }
            else
            {
                _isInAttack = false;
                AiState = enemyDist <= ChaseRange ? BotAiState.Chase : BotAiState.Wander;
            }

            switch (AiState)
            {
                case BotAiState.Wander: DoWander(dt, centerPos); break;
                case BotAiState.Chase:  DoChase(enemyPos);       break;
                case BotAiState.Attack: DoAttack();               break;
            }

            // ── 막힘 감지: 0.8초 내 0.35m 미만 이동 → 90° 방향 전환 ──────
            _stuckTimer += dt;
            if (_stuckTimer >= 0.8f)
            {
                float sx = Position.x - _lastPos.x, sz = Position.z - _lastPos.z;
                if (AiState != BotAiState.Attack && sx * sx + sz * sz < 0.12f)
                    _wanderAngle += (float)Math.PI * 0.5f * (_rng.NextDouble() > 0.5 ? 1 : -1);
                _lastPos    = Position;
                _stuckTimer = 0f;
            }

            // ── Leash 이탈 시 플레이어 위치로 복귀 ────────────────────────
            float dx = Position.x - centerPos.x, dz = Position.z - centerPos.z;
            if (dx * dx + dz * dz > (WanderLeashRadius * 3f) * (WanderLeashRadius * 3f))
            {
                Position     = centerPos;
                _heading     = (float)(2.0 * Math.PI * BotIndex / Math.Max(1, BotState.Count));
                _wanderAngle = _heading;
            }
        }

        // ── Reynolds Wander ─────────────────────────────────────────────────
        // 전방 투영 원 위의 목표점을 매 틱 조금씩 이동 → 자연스러운 랜덤 순찰
        private void DoWander(float dt, Vector3 centerPos)
        {
            _wanderAngle += (float)(_rng.NextDouble() * 2.0 - 1.0) * WanderJitter * dt;

            // 현재 heading 방향으로 WanderCircleDist만큼 투영한 원 중심
            float fwdX = (float)Math.Cos(_heading) * WanderCircleDist;
            float fwdZ = (float)Math.Sin(_heading) * WanderCircleDist;

            // 원 위의 목표 점
            float wx = Position.x + fwdX + (float)Math.Cos(_wanderAngle) * WanderCircleRadius;
            float wz = Position.z + fwdZ + (float)Math.Sin(_wanderAngle) * WanderCircleRadius;

            // Leash 초과 시 플레이어 방향으로 목표 당김
            float pdx = wx - centerPos.x, pdz = wz - centerPos.z;
            float pdist = (float)Math.Sqrt(pdx * pdx + pdz * pdz);
            if (pdist > WanderLeashRadius)
            {
                float inv = 1f / pdist;
                wx = centerPos.x + pdx * inv * (WanderLeashRadius - 1f);
                wz = centerPos.z + pdz * inv * (WanderLeashRadius - 1f);
            }

            float ddx = wx - Position.x, ddz = wz - Position.z;
            float len  = (float)Math.Sqrt(ddx * ddx + ddz * ddz);
            if (len > FormationDeadzone)
            {
                DesiredMoveDir = new Vector3(ddx / len, 0f, ddz / len);
                UpdateHeading();
            }
            else
            {
                DesiredMoveDir = Vector3.zero;
            }
        }

        private void DoChase(Vector3 enemyPos)
        {
            var diff  = new Vector3(enemyPos.x - Position.x, 0f, enemyPos.z - Position.z);
            float len = (float)Math.Sqrt(diff.x * diff.x + diff.z * diff.z);
            if (len > 0.01f)
            {
                DesiredMoveDir = new Vector3(diff.x / len, 0f, diff.z / len);
                UpdateHeading();
            }
            else
            {
                DesiredMoveDir = Vector3.zero;
            }
        }

        private void DoAttack()
        {
            DesiredMoveDir = Vector3.zero;
            if (_attackCooldown <= 0f) { ShouldAttack = true; _attackCooldown = AttackCooldownVal; }
        }

        // 이동 방향에서 heading(방위각) 갱신
        private void UpdateHeading()
        {
            if (DesiredMoveDir.x != 0f || DesiredMoveDir.z != 0f)
                _heading = (float)Math.Atan2(DesiredMoveDir.z, DesiredMoveDir.x);
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
