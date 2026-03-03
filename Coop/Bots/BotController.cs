using System;
using UnityEngine;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Bots
{
    /// <summary>순수 C# 이동 클래스 (MonoBehaviour 아님). 스레드 안전한 System.Random 사용.</summary>
    public class BotController
    {
        public ulong   BotId;
        public int     BotIndex;
        public Vector3 Position;
        public float   CurrentHp = 100f, MaxHp = 100f;
        public int     Level = 1;
        public float   Experience = 0f, ExperienceCap = 100f;

        private Vector3 _targetPos;
        private float   _wanderTimer;
        private const float WanderRadius = 8f, WalkSpeed = 3f, WanderInterval = 3f;
        private static readonly System.Random s_rng = new();

        public BotController(int index, Vector3 spawnPos)
        {
            BotIndex   = index;
            BotId      = BotState.BotSteamIds[index];
            Position   = spawnPos;
            _targetPos = spawnPos;
            _wanderTimer = WanderInterval; // 즉시 첫 목표 선정
        }

        public void Tick(float dt, Vector3 centerPos)
        {
            // 목표 재선정 (매 3초)
            _wanderTimer += dt;
            if (_wanderTimer >= WanderInterval)
            {
                _wanderTimer = 0f;
                float angle  = (float)(s_rng.NextDouble() * Math.PI * 2.0);
                float radius = (float)(s_rng.NextDouble() * WanderRadius);
                _targetPos = new Vector3(
                    centerPos.x + (float)Math.Cos(angle) * radius,
                    centerPos.y,   // Y는 플레이어와 동일 (NavMesh 없이 지형 무시)
                    centerPos.z + (float)Math.Sin(angle) * radius);
            }

            // 목표를 향해 이동
            var diff = new Vector3(_targetPos.x - Position.x, 0f, _targetPos.z - Position.z);
            float distSq = diff.x * diff.x + diff.z * diff.z;
            if (distSq > 0.001f)
            {
                float step = WalkSpeed * dt / (float)Math.Sqrt(distSq);
                if (step > 1f) step = 1f;
                Position.x += diff.x * step;
                Position.z += diff.z * step;
            }

            // 플레이어에서 3× 반경 이탈 시 리셋 (씬 전환 대비)
            float dx = Position.x - centerPos.x, dz = Position.z - centerPos.z;
            if (dx*dx + dz*dz > (WanderRadius*3f)*(WanderRadius*3f))
                Position = _targetPos = centerPos;
        }

        public PlayerStatePacket ToPacket() => new()
        {
            SteamId      = BotId,
            PosX         = Position.x,
            PosY         = Position.y,
            PosZ         = Position.z,
            CurrentHp    = CurrentHp,
            MaxHp        = MaxHp,
            Level        = Level,
            Experience   = Experience,
            ExperienceCap = ExperienceCap,
        };
    }
}
