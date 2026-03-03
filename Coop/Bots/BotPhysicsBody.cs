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
        private const float EnemyDetectRange  =  15f;

        private static readonly System.Random s_rng = new();

        // BotManager가 읽는 적 감지 결과
        public Health?  NearestEnemy     { get; private set; }
        public Vector3  NearestEnemyPos  { get; private set; }
        public float    NearestEnemyDist { get; private set; } = float.MaxValue;

        public Vector3 position => transform.position;

        public void Init(ulong botId)
        {
            _botId = botId;
            Instances[botId] = this;

            _cc = gameObject.AddComponent<CharacterController>();
            if (_cc != null)
            {
                _cc.height     = 1.8f;
                _cc.radius     = 0.25f;
                _cc.center     = new Vector3(0f, 0.9f, 0f);
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

            // CharacterController 이동
            _cc.Move(new Vector3(
                _desiredMoveDir.x * MoveSpeed * Time.deltaTime,
                _verticalVelocity * Time.deltaTime,
                _desiredMoveDir.z * MoveSpeed * Time.deltaTime));

            // 적 감지 스캔 (0.2초 간격)
            _enemyScanTimer += Time.deltaTime;
            if (_enemyScanTimer >= EnemyScanInterval)
            {
                _enemyScanTimer = 0f;
                ScanForEnemies();
            }
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
                var ep = h.gameObject.transform.position;
                float dx = ep.x - myPos.x, dy = ep.y - myPos.y, dz = ep.z - myPos.z;
                float dist = (float)Math.Sqrt(dx*dx + dy*dy + dz*dz);
                if (dist < bestDist) { bestDist = dist; best = h; bestPos = ep; }
            }

            NearestEnemy     = best;
            NearestEnemyPos  = bestPos;
            NearestEnemyDist = best != null ? bestDist : float.MaxValue;
        }
    }
}
