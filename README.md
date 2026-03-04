# Bloodshed Mod Toolkit

> A comprehensive BepInEx 6.x IL2CPP mod for **Bloodshed** (Steam, Windows) —
> cheats, balance tweaks, Steam co-op, and AI bot companions, all in one in-game menu.

[![Build](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/myso-kr/bloodshed-mod-toolkit)](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Languages:** [한국어](README.ko.md) · [日本語](README.ja.md) · [中文](README.zh-CN.md)

---

## Overview

Press **F5** in-game to open or close the mod menu.
The menu automatically adapts to your **current in-game language**.

The menu has four tabs:

| Tab | Contents |
|-----|----------|
| **CHEATS** | Survival/economy/combat/movement toggles and action buttons |
| **TWEAKS** | Difficulty presets and fine-grained balance sliders |
| **CO-OP** | Steam P2P lobby — host, join, friends list, XP share, mission gate |
| **BOTS** | AI bot companions (1–3 bots) |

---

## Features

### CHEATS Tab

#### Survival

| Toggle | Effect |
|--------|--------|
| God Mode | Player takes no damage |
| Max Stats | HP continuously restored to 99,999 |
| Infinite Revive | Revival count locked at 99 |
| Infinite Away | Away-ability count locked at 99 |

#### Economy

| Toggle | Effect |
|--------|--------|
| Infinite Gems | Gems never fall below 999,999 |
| Infinite Skull Coins | Skull Coins never fall below 999 |

#### Combat

| Toggle | Effect |
|--------|--------|
| One-Shot Kill | Outgoing damage to enemies ×9,999 |
| No Cooldown | Skill cooldown stat maximized |
| Rapid Fire | Weapon fire cooldown removed |
| No Recoil | Weapon recoil zeroed out |
| Perfect Aim | No bullet spread; aim precision never degrades |
| No Reload | Magazine never runs out; reload is cancelled |

#### Movement

| Toggle | Effect |
|--------|--------|
| Speed Hack | Movement speed multiplier (slider: ×1.0 – ×20.0) |

#### Action Buttons

| Button | Effect |
|--------|--------|
| Force Level Up | Adds exactly enough XP to trigger one level-up |
| Add 999,999 Gems | Instantly grants 999,999 gems |
| Skull Coins +999 | Instantly grants 999 Skull Coins |
| Full Heal | Restores HP to maximum |
| All Cheats OFF | Resets every toggle to off |

#### Overlay Position

Status Panel and DPS Panel can be pinned to **Top-Left**, **Top-Center**, or **Top-Right**, or hidden entirely.

---

### TWEAKS Tab

Apply a difficulty **preset** with one click, or fine-tune each multiplier individually with sliders.

#### Presets

| Preset | Summary |
|--------|---------|
| **Mortal** | Easy — stronger player, weaker enemies, fewer spawns |
| **Hunter** | Default — all values at ×1.00 |
| **Slayer** | Hard — tougher enemies, 50 % more spawns |
| **Demon** | Very Hard — enemies deal 2× damage, HP ×2.5, spawns ×2 |
| **Apocalypse** | Extreme — enemies deal 3× damage, HP ×4, spawns ×3 |

#### Individual Sliders

| Category | Parameter | Range |
|----------|-----------|-------|
| Player | HP Multiplier | ×0.10 – ×4.00 |
| Player | Speed Multiplier | ×0.50 – ×3.00 |
| Weapon | Damage Multiplier | ×0.50 – ×3.00 |
| Weapon | Fire Rate Multiplier | ×0.50 – ×3.00 |
| Weapon | Reload Speed Multiplier | ×0.50 – ×3.00 |
| Enemy | HP Multiplier | ×0.25 – ×5.00 |
| Enemy | Speed Multiplier | ×0.25 – ×3.00 |
| Enemy | Damage Multiplier | ×0.25 – ×5.00 |
| Spawn | Count Multiplier | ×0.25 – ×4.00 |

---

### CO-OP Tab

Adds **Steam P2P co-op** for up to 4 players using Steam lobbies.
All players must have the mod installed.

#### How to play

1. **Host** — Click **Create Lobby** in the CO-OP tab.
2. **Guest** — Copy the Lobby ID from the host, paste it into the Join field, and click **Join**.
   Alternatively, use the **Friends** section to join or invite friends directly.

#### XP Share Modes

| Mode | Behavior |
|------|----------|
| Independent | Each player earns XP separately |
| Replicate | Guest receives the same XP as the host (default) |
| Split | Host XP is halved and shared with the guest |

#### Mission Gate

When the host enters a mission, guests are held at a loading screen until the host's signal arrives,
preventing desync from independent scene loads.

---

### BOTS Tab

Spawns **1 to 3 AI bot companions** that follow the player and display their status (level, HP, position).

---

### Hotkeys

| Key | Action |
|-----|--------|
| **F5** | Open / close mod menu |
| **F6** | Full heal |
| **F7** | Force level up |

---

## Requirements

- **Bloodshed** — Steam, Windows 64-bit
- **BepInEx 6.x (IL2CPP build, Windows x64)**
  Download the latest `BepInEx_win-x64_*.zip` from the bleeding-edge build server:
  <https://builds.bepinex.dev/projects/bepinex_be>
  Use build **be.697 or later** (required for IL2CPP metadata v31).

---

## Installation

### Option A — Pre-built release (recommended)

1. Download the latest **`BloodshedModToolkit_vX.X.X.zip`** from [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases).
2. If BepInEx is not yet installed:
   - Extract the BepInEx zip into your `Bloodshed/` game folder so that `BepInEx/` appears at the root.
   - Launch the game once (a black console window should appear briefly), then quit — this generates the interop assemblies.
3. Extract the release zip. It contains a pre-built `BepInEx/plugins/` directory.
   Merge it into your Bloodshed game folder.
4. Launch Bloodshed. Press **F5** to open the mod menu.

### Option B — Manual DLL placement

Copy `BloodshedModToolkit.dll` to:

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

---

## Building from Source

### Prerequisites

- .NET 6 SDK
- BepInEx 6.x installed and the game launched at least once (to generate interop assemblies)

### Steps

```bash
git clone https://github.com/myso-kr/bloodshed-mod-toolkit.git
cd bloodshed-mod-toolkit

# Copy the local config template and set your BepInEx path
cp Directory.Build.props.example Directory.Build.props
# Edit Directory.Build.props — set BepInExPath to your BepInEx installation folder

dotnet build -c Release
# Output: bin/Release/net6.0/BloodshedModToolkit.dll
```

Copy the output DLL to `BepInEx/plugins/`.

### CI Setup (GitHub Actions)

| Secret | How to create |
|--------|---------------|
| `GAME_LIBS_B64` | Zip the `BepInEx/interop/` folder after running the game once, then base64-encode it: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("interop.zip"))` |

Without `GAME_LIBS_B64` the build step is skipped (stub build only — not deployable).

---

## Technical Notes

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

---

## License

[MIT](LICENSE)
