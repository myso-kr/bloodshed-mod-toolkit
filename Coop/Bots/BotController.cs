using System;
using System.Collections.Generic;
using UnityEngine;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Bots
{
    public enum BotAiState { Wander, Chase, Attack }

    /// <summary>
    /// 순수 C# AI 로직. 이동 실행은 BotPhysicsBody에 위임.
    ///
    /// 경로탐색 우선순위:
    ///   1순위: NavGrid.FindPath (A*) — 항아리·미로 지형 탈출 포함
    ///   2순위: Reynolds Wander / 직진 Chase (NavGrid 미스캔 시 폴백)
    /// </summary>
    public class BotController
    {
        public ulong   BotId;
        public int     BotIndex;
        public Vector3 Position;
        public float   CurrentHp = 100f, MaxHp = 100f;
        public int     Level = 1;
        public float   Experience = 0f, ExperienceCap = 100f;

        public WeaponClass WeaponClass    = WeaponClass.Melee;
        public BotAiState  AiState        = BotAiState.Wander;
        public Vector3     DesiredMoveDir = Vector3.zero;
        public bool        ShouldAttack   = false;
        private float      _attackCooldown = 0f;

        // ── 내비게이션 ────────────────────────────────────────────────────────
        private static readonly System.Random _rng = new();

        // A* 경로 추종 상태
        private List<Vector3>? _path;
        private int     _pathIdx;
        private Vector3 _pathTarget;        // 마지막 경로 계산 목표 위치
        private float   _wanderPathTimer = 0f; // Wander 목표 갱신 타이머

        // Reynolds Wander 폴백 상태
        private float   _wanderAngle = 0f;
        private float   _heading     = 0f;

        // 막힘 감지
        private Vector3 _lastPos;
        private float   _stuckTimer  = 0f;
        private bool    _isInAttack  = false; // 히스테리시스 플래그

        private const float WanderPathInterval  = 4f;   // Wander 목표 갱신 주기 (초)
        private const float WanderJitter        = 1.8f; // Reynolds wander 각도 지터 (rad/s)
        private const float WanderCircleDist    = 2.5f;
        private const float WanderCircleRadius  = 1.5f;
        private const float WanderLeashRadius   = 7f;   // 플레이어 이탈 한계 (m)
        private const float FormationDeadzone   = 0.8f;
        private const float WaypointReachRadius = 1.5f; // 웨이포인트 도달 반경 (m)

        // ── WeaponClass별 전투 파라미터 ───────────────────────────────────────
        private float AttackRange => WeaponClass switch {
            WeaponClass.Melee    => 2.0f,
            WeaponClass.Pistol   => 8.0f,
            WeaponClass.Rifle    => 15.0f,
            WeaponClass.Launcher => 20.0f,
            _                    => 4.0f,
        };
        // 감지 반경: 공격 사거리 × 2.5 + 4m, 상한 22m
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
            int total = Math.Max(1, BotState.Count);
            _heading     = (float)(2.0 * Math.PI * index / total);
            _wanderAngle = _heading;
        }

        public void Tick(float dt, Vector3 centerPos, Vector3 enemyPos, float enemyDist)
        {
            ShouldAttack = false;
            if (_attackCooldown > 0f) _attackCooldown -= dt;

            // ── 상태 전환 (히스테리시스 밴드) ─────────────────────────────
            if (enemyDist <= AttackRange)
            {
                AiState     = BotAiState.Attack;
                _isInAttack = true;
            }
            else if (_isInAttack && enemyDist <= AttackRange * 1.6f)
            {
                AiState = BotAiState.Attack; // 히스테리시스 존: 빠른 진동 방지
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

            // ── 막힘 감지: 0.8초 이내 0.35m 미만 이동 → 경로 무효화 ──────
            _stuckTimer += dt;
            if (_stuckTimer >= 0.8f)
            {
                float sx = Position.x - _lastPos.x, sz = Position.z - _lastPos.z;
                if (AiState != BotAiState.Attack && sx * sx + sz * sz < 0.12f)
                {
                    _path        = null; // A* 재계획 강제
                    _wanderAngle += (float)Math.PI * 0.5f * (_rng.NextDouble() > 0.5 ? 1 : -1);
                }
                _lastPos    = Position;
                _stuckTimer = 0f;
            }

            // ── Leash 이탈 시 플레이어 위치로 복귀 ────────────────────────
            float dx = Position.x - centerPos.x, dz = Position.z - centerPos.z;
            if (dx * dx + dz * dz > (WanderLeashRadius * 3f) * (WanderLeashRadius * 3f))
            {
                Position     = centerPos;
                _path        = null;
                _heading     = (float)(2.0 * Math.PI * BotIndex / Math.Max(1, BotState.Count));
                _wanderAngle = _heading;
            }
        }

        // ── Chase: A* 경로 → 직진 폴백 ──────────────────────────────────────
        private void DoChase(Vector3 enemyPos)
        {
            // 적이 3m 이상 이동했거나 경로가 없으면 재계획
            float ex = enemyPos.x - _pathTarget.x, ez = enemyPos.z - _pathTarget.z;
            if (_path == null || ex * ex + ez * ez > 9f)
            {
                _path       = NavGrid.FindPath(Position, enemyPos);
                _pathIdx    = 0;
                _pathTarget = enemyPos;
            }

            if (FollowPath()) return;

            // 폴백: 직진 (NavGrid 미스캔 또는 경로 없음)
            var diff = new Vector3(enemyPos.x - Position.x, 0f, enemyPos.z - Position.z);
            float len = (float)Math.Sqrt(diff.x * diff.x + diff.z * diff.z);
            if (len > 0.01f) { DesiredMoveDir = new Vector3(diff.x / len, 0f, diff.z / len); UpdateHeading(); }
            else DesiredMoveDir = Vector3.zero;
        }

        // ── Wander: A* 랜덤 목표 → Reynolds Wander 폴백 ────────────────────
        private void DoWander(float dt, Vector3 centerPos)
        {
            _wanderPathTimer -= dt;
            bool pathDone = _path == null || _pathIdx >= _path.Count;

            if (pathDone || _wanderPathTimer <= 0f)
            {
                var dest = NavGrid.GetRandomWalkableNear(centerPos, WanderLeashRadius);
                if (dest.HasValue)
                {
                    _path            = NavGrid.FindPath(Position, dest.Value);
                    _pathIdx         = 0;
                    _pathTarget      = dest.Value;
                    _wanderPathTimer = WanderPathInterval;
                }
            }

            if (FollowPath()) return;

            // 폴백: Reynolds Wander (NavGrid 미스캔 시)
            _wanderAngle += (float)(_rng.NextDouble() * 2.0 - 1.0) * WanderJitter * dt;

            float fwdX = (float)Math.Cos(_heading) * WanderCircleDist;
            float fwdZ = (float)Math.Sin(_heading) * WanderCircleDist;
            float wx   = Position.x + fwdX + (float)Math.Cos(_wanderAngle) * WanderCircleRadius;
            float wz   = Position.z + fwdZ + (float)Math.Sin(_wanderAngle) * WanderCircleRadius;

            float pdx   = wx - centerPos.x, pdz = wz - centerPos.z;
            float pdist = (float)Math.Sqrt(pdx * pdx + pdz * pdz);
            if (pdist > WanderLeashRadius)
            {
                float inv = 1f / pdist;
                wx = centerPos.x + pdx * inv * (WanderLeashRadius - 1f);
                wz = centerPos.z + pdz * inv * (WanderLeashRadius - 1f);
            }

            float ddx = wx - Position.x, ddz = wz - Position.z;
            float dlen = (float)Math.Sqrt(ddx * ddx + ddz * ddz);
            if (dlen > FormationDeadzone)
            { DesiredMoveDir = new Vector3(ddx / dlen, 0f, ddz / dlen); UpdateHeading(); }
            else DesiredMoveDir = Vector3.zero;
        }

        private void DoAttack()
        {
            DesiredMoveDir = Vector3.zero;
            if (_attackCooldown <= 0f) { ShouldAttack = true; _attackCooldown = AttackCooldownVal; }
        }

        // A* 경로 추종. 다음 웨이포인트 방향으로 이동, 경로 완료 시 false 반환
        private bool FollowPath()
        {
            if (_path == null || _pathIdx >= _path.Count) return false;

            var   wp      = _path[_pathIdx];
            float dx      = wp.x - Position.x, dz = wp.z - Position.z;
            float distSq  = dx * dx + dz * dz;

            // 웨이포인트 도달 → 다음으로 진행
            if (distSq < WaypointReachRadius * WaypointReachRadius)
            {
                _pathIdx++;
                if (_pathIdx >= _path.Count) return false;
                wp   = _path[_pathIdx];
                dx   = wp.x - Position.x;
                dz   = wp.z - Position.z;
                distSq = dx * dx + dz * dz;
            }

            float len = (float)Math.Sqrt(distSq);
            if (len < 0.01f) return false;

            DesiredMoveDir = new Vector3(dx / len, 0f, dz / len);
            UpdateHeading();
            return true;
        }

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
