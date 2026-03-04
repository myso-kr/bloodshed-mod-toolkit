---
title: "Technical Notes — Bloodshed Mod Toolkit"
description: >-
  Implementation details: Harmony hooks used for each cheat and tweak feature in Bloodshed.
  Disclaimer for co-op and cheat usage.
lang: en
---

[← Overview](../overview/en.md) · [Features](../features/en.md) · [Installation](../installation/en.md) · [License](../license/en.md)

**Language:** **English** · [한국어](ko.md) · [日本語](ja.md) · [中文](zh-CN.md)

---

# Technical Notes

| Feature | Hook |
|---------|------|
| God Mode | `PlayerStats.TakeDamage(float, GameObject)` Prefix → skip |
| Infinite Gems | `PlayerStats.SetMoney(float)` Postfix + `PersistentData.currentMoney` per frame |
| Speed Hack | `Q3PlayerController.GetPlayerSpeed/ForwardSpeed/StrafeSpeed` Postfix → multiply |
| One-Shot Kill | `Health.Damage(float, …)` Prefix → ×9,999 for non-player entities |
| No Cooldown | `ActiveAbilityHandler.ProcessActiveAbilities()` Postfix → zero all timers |
| No Reload | `WeaponItem.GetCurrentAmmo` Prefix → return max |
| Rapid Fire | `ShotAction.SetCooldownEnd` Postfix → reset to 0 |
| No Recoil | `WeaponItem.GetRecoilTotal` Prefix → return 0 |
| Perfect Aim | `ShotAction.GetSpreadDirection` Prefix → return normalized; `AimPrecisionHandler.ReducePrecision` Prefix → skip |
| Enemy Speed Tweak | `EnemyAbilityController.SetBehaviorWalkable(float)` Postfix → multiply |
| Spawn Count Tweak | `SpawnProcessor.GetMaxEnemyCount()` Postfix + `SpawnDirector.SpawnEnemies(…)` Prefix → multiply |
| Player HP Tweak | `PlayerStats.RecalculateStats()` Postfix → multiply MaxHp |

---

## ⚠️ Disclaimer

The co-op feature connects only with **friends you invite** via Steam.
Cheats (God Mode, One-Shot Kill, etc.) should be used only in **private sessions** with the consent of all players.
Using cheats against other players in public lobbies may violate the game's Terms of Service.
The author is not responsible for any consequences arising from misuse.
