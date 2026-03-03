namespace BloodshedModToolkit.Tweaks
{
    /// <summary>
    /// 모든 밸런스 트윅 파라미터 집합.
    /// 기본값은 1.0 (게임 기본값 그대로).
    /// </summary>
    public class TweakConfig
    {
        // ── 플레이어 ─────────────────────────────────────────────────────────────

        /// <summary>최대 체력 배율 — RecalculateStats Postfix로 MaxHp에 적용</summary>
        public float PlayerHpMult { get; set; } = 1f;

        /// <summary>이동속도 배율 — Accelerate/AirControl Prefix로 targetSpeed에 적용</summary>
        public float PlayerSpeedMult { get; set; } = 1f;

        // ── 무기 ──────────────────────────────────────────────────────────────────

        /// <summary>무기 데미지 배율 — GetDamageTotal Postfix</summary>
        public float WeaponDamageMult { get; set; } = 1f;

        /// <summary>
        /// 발사속도 배율 — GetCooldownTotal 결과를 나눔.
        /// 값이 클수록 쿨다운이 짧아져 발사속도가 빠릅니다.
        /// </summary>
        public float WeaponFireRateMult { get; set; } = 1f;

        /// <summary>
        /// 장전속도 배율 — GetReloadDurationTotal 결과를 나눔.
        /// 값이 클수록 장전 시간이 짧아집니다.
        /// </summary>
        public float WeaponReloadSpeedMult { get; set; } = 1f;

        // ── 에너미 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 에너미 체력 배율 — Health.Damage Prefix에서 에너미가 받는 피해를 역수로 스케일.
        /// 값이 클수록 에너미가 더 많은 피해를 버팁니다 (실질적으로 HP 증가).
        /// </summary>
        public float EnemyHpMult { get; set; } = 1f;

        /// <summary>에너미 이동속도 배율 — EnemyAbilityController.SetBehaviorWalkable Prefix</summary>
        public float EnemySpeedMult { get; set; } = 1f;

        /// <summary>에너미가 플레이어에게 가하는 데미지 배율 — Health.Damage Prefix (isPlayer 체크)</summary>
        public float EnemyDamageMult { get; set; } = 1f;

        // ── 스폰 ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// 최대 에너미 수 및 스폰 수량 배율.
        /// SpawnProcessor.GetMaxEnemyCount Postfix + SpawnDirector.SpawnEnemies Prefix에 적용.
        /// </summary>
        public float SpawnCountMult { get; set; } = 1f;
    }
}
