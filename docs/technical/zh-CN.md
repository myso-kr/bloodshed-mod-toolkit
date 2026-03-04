---
title: "技术说明 — Bloodshed Mod Toolkit"
description: >-
  实现细节: Bloodshed各作弊和调整功能所用的Harmony钩子。
  关于联机和作弊使用的免责声明。
lang: zh-CN
---

[← 概述](../overview/zh-CN.md) · [功能](../features/zh-CN.md) · [安装](../installation/zh-CN.md) · [许可证](../license/zh-CN.md)

**语言:** [English](en.md) · [한국어](ko.md) · [日本語](ja.md) · **中文**

---

# 技术说明

| 功能 | 钩子 |
|------|------|
| 无敌模式 | `PlayerStats.TakeDamage(float, GameObject)` Prefix → 跳过 |
| 无限宝石 | `PlayerStats.SetMoney(float)` Postfix + `PersistentData.currentMoney` 每帧同步 |
| 移速倍率 | `Q3PlayerController.GetPlayerSpeed/ForwardSpeed/StrafeSpeed` Postfix → 乘以倍率 |
| 一击必杀 | `Health.Damage(float, …)` Prefix → 非玩家目标 ×9,999 |
| 冷却消除 | `ActiveAbilityHandler.ProcessActiveAbilities()` Postfix → 所有计时器归零 |
| 无需换弹 | `WeaponItem.GetCurrentAmmo` Prefix → 返回最大值 |
| 速射 | `ShotAction.SetCooldownEnd` Postfix → 强制为 0 |
| 无后坐力 | `WeaponItem.GetRecoilTotal` Prefix → 返回 0 |
| 完美瞄准 | `ShotAction.GetSpreadDirection` Prefix → 返回 normalized; `AimPrecisionHandler.ReducePrecision` Prefix → 跳过 |
| 敌人速度调整 | `EnemyAbilityController.SetBehaviorWalkable(float)` Postfix → 乘以倍率 |
| 刷怪数量调整 | `SpawnProcessor.GetMaxEnemyCount()` Postfix + `SpawnDirector.SpawnEnemies(…)` Prefix → 乘以倍率 |
| 玩家 HP 调整 | `PlayerStats.RecalculateStats()` Postfix → MaxHp 乘以倍率 |

---

## ⚠️ 免责声明

Co-op 功能仅与**通过 Steam 直接邀请的好友**连接。
无敌、一击必杀等作弊功能请仅在**所有参与者同意的私人房间中**使用。
在公开房间或以影响其他玩家的方式使用作弊功能，可能违反游戏服务条款。
因不当使用而产生的任何后果，作者概不负责。
