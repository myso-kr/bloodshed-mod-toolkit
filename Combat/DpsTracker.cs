using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;

namespace BloodshedModToolkit.Combat
{
    /// <summary>
    /// 롤링 윈도우(3초) 기반 DPS 추적기.
    /// CheatMenu.Update() 에서 매 프레임 Tick() 을 호출해야 합니다.
    /// </summary>
    public static class DpsTracker
    {
        // ── 설정 ─────────────────────────────────────────────────────────────
        public const float CombatGap    = 4f;    // 전투 공백 판정 (초)
        private const float WindowSecs  = 3f;    // DPS 롤링 윈도우
        private const float PeakHoldSec = 10f;   // 피크 DPS 유지 시간

        // ── 히트 버퍼 ─────────────────────────────────────────────────────────
        private struct Hit { public float T; public float D; }
        private static readonly Queue<Hit> _q = new();

        // ── 공개 속성 ─────────────────────────────────────────────────────────
        public static float CurrentDps  { get; private set; }
        public static float PeakDps     { get; private set; }
        public static float TotalDamage { get; private set; }
        public static int   HitCount    { get; private set; }

        private static float _lastHit  = -999f;
        private static float _peakTime = -999f;

        public static float TimeSinceHit => Time.time - _lastHit;
        public static bool  IsActive     => Time.time - _lastHit < CombatGap;

        /// <summary>PeakHoldSec 이내 기록된 경우에만 유효한 피크 반환.</summary>
        public static float ValidPeakDps =>
            Time.time - _peakTime < PeakHoldSec ? PeakDps : 0f;

        // ── API ───────────────────────────────────────────────────────────────
        /// <summary>피해량 기록 — DpsMeterPatch 에서 호출.</summary>
        public static void Record(float dmg)
        {
            if (dmg <= 0f) return;
            float now = Time.time;
            _q.Enqueue(new Hit { T = now, D = dmg });
            TotalDamage += dmg;
            HitCount++;
            _lastHit = now;
        }

        /// <summary>매 프레임 호출 — 오래된 히트 정리 및 DPS 재계산.</summary>
        public static void Tick()
        {
            float now = Time.time;
            while (_q.Count > 0 && now - _q.Peek().T > WindowSecs)
                _q.Dequeue();

            if (_q.Count == 0) { CurrentDps = 0f; return; }

            // 전투 시작 직후처럼 윈도우보다 짧은 구간은 실제 경과 시간으로 나눔
            float span = Mathf.Min(now - _q.Peek().T, WindowSecs);
            if (span < 0.05f) { CurrentDps = 0f; return; }

            float sum = 0f;
            foreach (var h in _q) sum += h.D;
            CurrentDps = sum / span;

            if (CurrentDps > PeakDps)
            {
                PeakDps   = CurrentDps;
                _peakTime = now;
            }
        }

        /// <summary>전투 통계 초기화.</summary>
        public static void Reset()
        {
            _q.Clear();
            CurrentDps = PeakDps = TotalDamage = 0f;
            HitCount   = 0;
            _lastHit   = _peakTime = -999f;
        }
    }

    // ── Harmony 패치 — Health.Damage Postfix ─────────────────────────────────
    /// <summary>
    /// 적(non-player)에게 가해진 모든 피해를 DpsTracker 에 기록.
    /// EnemyDeathEventPatch / GuestDamageRequestPatch 와 동일 메서드를 Postfix 로 패치하며,
    /// Harmony 는 복수 패치 클래스를 순서대로 실행하므로 충돌 없음.
    /// </summary>
    [HarmonyPatch(typeof(Health), "Damage",
        new Type[]
        {
            typeof(float), typeof(GameObject),
            typeof(float), typeof(float),
            typeof(Vector3), typeof(Vector3), typeof(bool)
        })]
    public static class DpsMeterPatch
    {
        static void Postfix(Health __instance, float damage)
        {
            if (__instance.isPlayer) return;
            DpsTracker.Record(damage);
        }
    }
}
