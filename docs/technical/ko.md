---
title: "기술 노트 — Bloodshed Mod Toolkit"
description: >-
  구현 상세: Bloodshed 각 치트 및 트윅 기능에 사용된 Harmony 훅.
  코-업 및 치트 사용 주의사항.
lang: ko
---

# 기술 노트

| 기능 | 후킹 방식 |
|------|-----------|
| 무적 모드 | `PlayerStats.TakeDamage(float, GameObject)` Prefix → 스킵 |
| 무한 젬 | `PlayerStats.SetMoney(float)` Postfix + `PersistentData.currentMoney` 매 프레임 |
| 이동속도 배율 | `Q3PlayerController.GetPlayerSpeed/ForwardSpeed/StrafeSpeed` Postfix → 배율 적용 |
| 원샷킬 | `Health.Damage(float, …)` Prefix → 비플레이어 대상 ×9,999 |
| 쿨다운 제거 | `ActiveAbilityHandler.ProcessActiveAbilities()` Postfix → 타이머 0 |
| 장전 없음 | `WeaponItem.GetCurrentAmmo` Prefix → 최대값 반환 |
| 속사 | `ShotAction.SetCooldownEnd` Postfix → 0 강제 |
| 무반동 | `WeaponItem.GetRecoilTotal` Prefix → 0 반환 |
| 완벽한 조준 | `ShotAction.GetSpreadDirection` Prefix → normalized 반환; `AimPrecisionHandler.ReducePrecision` Prefix → 스킵 |
| 에너미 속도 트윅 | `EnemyAbilityController.SetBehaviorWalkable(float)` Postfix → 배율 적용 |
| 스폰 수량 트윅 | `SpawnProcessor.GetMaxEnemyCount()` Postfix + `SpawnDirector.SpawnEnemies(…)` Prefix → 배율 적용 |
| 플레이어 HP 트윅 | `PlayerStats.RecalculateStats()` Postfix → MaxHp 배율 적용 |

---

## ⚠️ 주의사항

코-업 기능은 **Steam을 통해 직접 초대한 친구**와만 연결됩니다.
무적, 원샷킬 등의 치트는 **모든 참가자가 동의한 비공개 세션**에서만 사용하세요.
공개 로비 등 다른 플레이어에게 영향을 미치는 방식으로 치트를 사용하면 게임 이용약관에 위반될 수 있습니다.
부적절한 사용으로 인한 결과에 대해 제작자는 책임을 지지 않습니다.
