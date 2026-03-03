using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Renderer
{
    public class BotAvatarAnimator : MonoBehaviour
    {
        public BotAvatarAnimator(IntPtr ptr) : base(ptr) { }

        public enum AvatarMode { Procedural, GameModel }

        public static readonly Dictionary<ulong, BotAvatarAnimator> Instances = new();
        private ulong _botId;

        // ── 모드 ──
        public AvatarMode Mode = AvatarMode.Procedural;

        // ── GameModel 모드용 ──
        public Animator?  GameAnimator;
        // 파라미터 이름 (런타임 검색으로 확정)
        private string? _paramSpeed;
        private string? _paramGrounded;
        private string? _paramAttack;

        // ── Procedural 모드용 뼈대 ──
        // 회전 피벗 Transform (BotAvatarBuilder가 할당)
        public Transform? ThighL, ThighR;
        public Transform? ShinL,  ShinR;
        public Transform? ShoulderL, ShoulderR;
        public Transform? ForearmR;           // 공격 시 오른팔 전완부 회전
        public Transform? Torso;              // 숨쉬기용

        // ── 공통 상태 ──
        private float _moveSpeed;
        private bool  _isGrounded = true;
        private float _attackTimer = -1f;
        private const float AttackDuration = 0.4f;
        private const float MoveSpeedMax   = 4f;
        private const double TwoPI = Math.PI * 2.0;

        public void Init(ulong botId)
        {
            _botId = botId;
            Instances[botId] = this;
        }

        void OnDestroy() { Instances.Remove(_botId); }

        public void SetMoveSpeed(float s) => _moveSpeed = s;
        public void SetGrounded(bool g)   => _isGrounded = g;

        public void TriggerAttack()
        {
            _attackTimer = 0f;
            if (Mode == AvatarMode.GameModel && GameAnimator != null && _paramAttack != null)
                GameAnimator.SetTrigger(_paramAttack);
        }

        /// <summary>GameModel 모드 초기화 — Animator 파라미터 자동 탐색 후 저장</summary>
        public void InitGameModel(Animator anim)
        {
            GameAnimator = anim;
            anim.applyRootMotion = false;
            anim.cullingMode     = AnimatorCullingMode.AlwaysAnimate;

            // 파라미터 이름 탐색 (알려진 후보 매핑)
            foreach (var p in anim.parameters)
            {
                string n = p.name.ToLowerInvariant();
                if (_paramSpeed    == null && (n == "speed" || n == "movespeed" || n == "velocity"))
                    _paramSpeed    = p.name;
                if (_paramGrounded == null && (n == "isgrounded" || n == "grounded"))
                    _paramGrounded = p.name;
                if (_paramAttack   == null && (n == "attack" || n == "isattacking" || n == "doattack" || n == "triggerattack"))
                    _paramAttack   = p.name;
                Plugin.Log.LogInfo($"[BotAnim] Animator param: {p.name} ({p.type})");
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_attackTimer >= 0f) _attackTimer += dt;
            if (_attackTimer >= AttackDuration)  _attackTimer = -1f;

            if (Mode == AvatarMode.GameModel)
                UpdateGameModel();
            else
                UpdateProcedural();
        }

        // ── GameModel 모드 ───────────────────────────────────────────────────
        private void UpdateGameModel()
        {
            if (GameAnimator == null) return;
            if (_paramSpeed    != null) GameAnimator.SetFloat(_paramSpeed,   _moveSpeed);
            if (_paramGrounded != null) GameAnimator.SetBool(_paramGrounded, _isGrounded);
        }

        // ── Procedural 모드 ─────────────────────────────────────────────────
        private void UpdateProcedural()
        {
            float t     = Time.time;
            float spd   = Math.Min(_moveSpeed / MoveSpeedMax, 1f);
            float amp   = spd * 30f;                          // 최대 ±30°
            float freq  = 1.5f + spd * 0.5f;
            float phase = (float)(t * freq * TwoPI);

            // 점프 포즈 vs 보행
            if (!_isGrounded)
            {
                SetRot(ThighL,    15f, 0f, 0f);
                SetRot(ThighR,    15f, 0f, 0f);
                SetRot(ShinL,    -35f, 0f, 0f);
                SetRot(ShinR,    -35f, 0f, 0f);
                SetRot(ShoulderL,-25f,-10f,-15f);
                SetRot(ShoulderR,-25f, 10f, 15f);
            }
            else
            {
                float sin   = (float)Math.Sin(phase);
                float shinL = Math.Max(0f, sin);
                float shinR = Math.Max(0f, -sin);

                SetRot(ThighL,    sin * amp,  0f, 0f);
                SetRot(ThighR,   -sin * amp,  0f, 0f);
                SetRot(ShinL,   -shinL * amp * 0.5f, 0f, 0f);
                SetRot(ShinR,   -shinR * amp * 0.5f, 0f, 0f);
                // 팔은 다리와 반대 위상
                SetRot(ShoulderL, -sin * amp * 0.6f, 0f, 0f);
                SetRot(ShoulderR,  sin * amp * 0.6f, 0f, 0f);
            }

            // 공격: 오른팔 전완부 앞으로 스윙 (sin 곡선, 0.4초)
            if (_attackTimer >= 0f && ForearmR != null)
            {
                float p = _attackTimer / AttackDuration;
                float thrust = (float)Math.Sin(p * Math.PI) * 70f;
                SetRot(ForearmR, -thrust, 0f, 0f);
            }
            else if (ForearmR != null)
            {
                // 기본 자세: 팔꿈치 약간 굽힘
                SetRot(ForearmR, -10f, 0f, 0f);
            }
        }

        private static void SetRot(Transform? t, float x, float y, float z)
        {
            if (t != null) t.localRotation = Quaternion.Euler(x, y, z);
        }
    }
}
