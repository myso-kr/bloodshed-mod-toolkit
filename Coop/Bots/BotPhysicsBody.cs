using System;
using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Coop.Bots
{
    public class BotPhysicsBody : MonoBehaviour
    {
        public BotPhysicsBody(IntPtr ptr) : base(ptr) { }

        public static readonly Dictionary<ulong, BotPhysicsBody> Instances = new();

        private ulong _botId;
        private CharacterController? _cc;
        private float   _verticalVelocity = 0f;
        private float   _jumpTimer  = 0f;
        private float   _nextJumpAt = 5f;
        private Vector3 _desiredMoveDir = Vector3.zero;
        private float   _enemyScanTimer = 0f;

        private const float Gravity           = -20f;
        private const float JumpSpeed         =   6f;
        private const float JumpIntervalMin   =   4f;
        private const float JumpIntervalMax   =  10f;
        private const float MoveSpeed         =   4f;
        private const float EnemyScanInterval =   0.2f;
        private const float EnemyDetectRange  =  75f;  // 원거리 공격 적 대비 (15 × 5)

        private static readonly System.Random s_rng = new();

        // BotManager가 읽는 적 감지 결과
        public Health?  NearestEnemy     { get; private set; }
        public Vector3  NearestEnemyPos  { get; private set; }
        public float    NearestEnemyDist { get; private set; } = float.MaxValue;

        public Vector3 position    => transform.position;
        public bool    IsGrounded  => _cc != null && _cc.isGrounded;
        public float   CurrentSpeed =>
            (float)Math.Sqrt(_desiredMoveDir.x * _desiredMoveDir.x
                           + _desiredMoveDir.z * _desiredMoveDir.z) * MoveSpeed;

        public void Init(ulong botId)
        {
            _botId = botId;
            Instances[botId] = this;

            _cc = gameObject.AddComponent<CharacterController>();
            if (_cc != null)
            {
                _cc.height     = 1.8f;
                _cc.radius     = 0.25f;
                _cc.center     = new Vector3(0f, 0f, 0f);  // 기하 중심 = 물리 중심 → 아바타 Y 묻힘 방지
                _cc.stepOffset = 0.3f;
                _cc.slopeLimit = 45f;
            }
            _nextJumpAt = JumpIntervalMin +
                (float)(s_rng.NextDouble() * (JumpIntervalMax - JumpIntervalMin));
            Plugin.Log.LogInfo($"[BotPhysicsBody] Init botId={botId:X}");
        }

        void OnDestroy() { Instances.Remove(_botId); }

        public void SetMoveDir(Vector3 dir) => _desiredMoveDir = dir;

        void Update()
        {
            if (_cc == null) return;

            // 중력 + 점프
            if (_cc.isGrounded)
            {
                _verticalVelocity = -1f; // 접지 유지
                _jumpTimer += Time.deltaTime;
                if (_jumpTimer >= _nextJumpAt)
                {
                    _verticalVelocity = JumpSpeed;
                    _jumpTimer  = 0f;
                    _nextJumpAt = JumpIntervalMin +
                        (float)(s_rng.NextDouble() * (JumpIntervalMax - JumpIntervalMin));
                }
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            // 위스커 레이캐스트 장애물 회피 → CharacterController 이동
            var moveDir = AvoidObstacles(_desiredMoveDir);
            _cc.Move(new Vector3(
                moveDir.x * MoveSpeed * Time.deltaTime,
                _verticalVelocity * Time.deltaTime,
                moveDir.z * MoveSpeed * Time.deltaTime));

            // 적 감지 스캔 (0.2초 간격)
            _enemyScanTimer += Time.deltaTime;
            if (_enemyScanTimer >= EnemyScanInterval)
            {
                _enemyScanTimer = 0f;
                ScanForEnemies();
            }
        }

        // ── Whisker Raycast 장애물 회피 ─────────────────────────────────────
        // 정면 + 좌우 30° 총 3개 광선으로 벽·장애물 감지 → 회피 방향 블렌드
        private Vector3 AvoidObstacles(Vector3 desired)
        {
            if (desired.x == 0f && desired.z == 0f) return desired;

            float len = (float)Math.Sqrt(desired.x * desired.x + desired.z * desired.z);
            float nx  = desired.x / len, nz = desired.z / len;

            // 가슴 높이(0.9m)에서 수평 광선 발사 — 지면·천장 오탐 방지
            var origin = transform.position + new Vector3(0f, 0.9f, 0f);

            // cos30° ≈ 0.8660, sin30° = 0.5000
            const float CosA = 0.8660f, SinA = 0.5000f;
            const float CenterLen = 2.0f, SideLen = 1.5f;

            float steerX = 0f, steerZ = 0f;

            // 정면 위스커 — 충돌 시 역방향으로 강하게 밀어냄
            if (Physics.Raycast(origin, new Vector3(nx, 0f, nz), CenterLen))
            { steerX -= nx; steerZ -= nz; }

            // 우측 위스커 (CW 30°) — 충돌 시 좌로 스티어
            float rx = nx * CosA + nz * SinA, rz = -nx * SinA + nz * CosA;
            if (Physics.Raycast(origin, new Vector3(rx, 0f, rz), SideLen))
            { steerX -= rx * 0.7f; steerZ -= rz * 0.7f; }

            // 좌측 위스커 (CCW 30°) — 충돌 시 우로 스티어
            float lx = nx * CosA - nz * SinA, lz = nx * SinA + nz * CosA;
            if (Physics.Raycast(origin, new Vector3(lx, 0f, lz), SideLen))
            { steerX -= lx * 0.7f; steerZ -= lz * 0.7f; }

            if (steerX == 0f && steerZ == 0f) return desired;

            // 회피 벡터를 원하는 방향에 블렌드 후 정규화
            float blX = nx + steerX * 1.8f, blZ = nz + steerZ * 1.8f;
            float bl  = (float)Math.Sqrt(blX * blX + blZ * blZ);
            return bl > 0.001f ? new Vector3(blX / bl, 0f, blZ / bl) : desired;
        }

        private void ScanForEnemies()
        {
            var all = UnityEngine.Object.FindObjectsOfType<Health>();
            if (all == null) { NearestEnemy = null; NearestEnemyDist = float.MaxValue; return; }

            Health?  best     = null;
            float    bestDist = EnemyDetectRange;
            Vector3  bestPos  = Vector3.zero;
            var      myPos    = transform.position;

            foreach (var h in all)
            {
                if (h == null || h.isPlayer || h.currentHealth <= 0f) continue;
                var go = h.gameObject;
                if (go == null) continue;
                var tr = go.transform;
                if (tr == null) continue;
                var ep = tr.position;
                float dx = ep.x - myPos.x, dy = ep.y - myPos.y, dz = ep.z - myPos.z;
                float dist = (float)Math.Sqrt(dx*dx + dy*dy + dz*dz);
                if (dist < bestDist) { bestDist = dist; best = h; bestPos = ep; }
            }

            // 상태 변화 시 로그
            bool changed = (best == null) != (NearestEnemy == null);
            NearestEnemy     = best;
            NearestEnemyPos  = bestPos;
            NearestEnemyDist = best != null ? bestDist : float.MaxValue;
            if (changed)
                Plugin.Log.LogInfo($"[BotPhysicsBody {_botId:X8}] 적 감지 → {(best != null ? $"{bestDist:F1}m" : "없음")} (전체 Health {all.Length}개)");
        }
    }
}
