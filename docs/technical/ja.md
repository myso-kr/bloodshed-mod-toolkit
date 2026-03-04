---
title: "技術ノート — Bloodshed Mod Toolkit"
description: >-
  実装詳細: BloodshedのチートとトウィークにつかったHarmonyフック。
  Co-opとチート使用に関する免責事項。
lang: ja
---

[← 概要](../overview/ja.md) · [機能](../features/ja.md) · [インストール](../installation/ja.md) · [ライセンス](../license/ja.md)

**言語:** [English](en.md) · [한국어](ko.md) · **日本語** · [中文](zh-CN.md)

---

# 技術ノート

| 機能 | フック |
|------|--------|
| 無敵モード | `PlayerStats.TakeDamage(float, GameObject)` Prefix → スキップ |
| ジェム無限 | `PlayerStats.SetMoney(float)` Postfix + `PersistentData.currentMoney` 毎フレーム |
| 移動速度倍率 | `Q3PlayerController.GetPlayerSpeed/ForwardSpeed/StrafeSpeed` Postfix → 倍率適用 |
| 一撃必殺 | `Health.Damage(float, …)` Prefix → 非プレイヤー対象 ×9,999 |
| クールダウン除去 | `ActiveAbilityHandler.ProcessActiveAbilities()` Postfix → タイマー0 |
| リロードなし | `WeaponItem.GetCurrentAmmo` Prefix → 最大値返却 |
| 速射 | `ShotAction.SetCooldownEnd` Postfix → 0 強制 |
| 無反動 | `WeaponItem.GetRecoilTotal` Prefix → 0 返却 |
| 完璧な照準 | `ShotAction.GetSpreadDirection` Prefix → normalized 返却; `AimPrecisionHandler.ReducePrecision` Prefix → スキップ |
| 敵速度トウィーク | `EnemyAbilityController.SetBehaviorWalkable(float)` Postfix → 倍率適用 |
| スポーン数トウィーク | `SpawnProcessor.GetMaxEnemyCount()` Postfix + `SpawnDirector.SpawnEnemies(…)` Prefix → 倍率適用 |
| プレイヤーHPトウィーク | `PlayerStats.RecalculateStats()` Postfix → MaxHp に倍率適用 |

---

## ⚠️ 免責事項

Co-op機能は **Steamで直接招待した友人** とのみ接続します。
無敵・一撃必殺などのチートは、**全参加者が同意したプライベートセッションでのみ** 使用してください。
公開ロビーなど他のプレイヤーに影響する形でチートを使用した場合、ゲームの利用規約に違反する可能性があります。
不正使用による結果について、作者は一切の責任を負いません。
