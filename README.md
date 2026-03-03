# Bloodshed Cheat Mod

> A BepInEx 6.x IL2CPP plugin that adds an in-game cheat menu to **Bloodshed** (Steam, Windows).

[![Build](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/myso-kr/bloodshed-mod-toolkit)](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Languages / 언어:** [한국어](README.ko.md) · [日本語](README.ja.md) · [中文](README.zh-CN.md)

---

## Features

Click the **★ Cheat Mod** overlay in the top-right corner of the screen to toggle the menu.
The menu automatically adapts to the **game's current language** (19 languages supported).

### Toggles

| Cheat | Description |
|-------|-------------|
| God Mode | Player receives no damage |
| Infinite Gems | Gems never drop below 999,999 |
| Infinite Skull Coins | Skull Coins never drop below 999 |
| Max Stats | All player stats set to maximum |
| Speed Hack | Movement speed multiplier (slider: ×1 – ×20) |
| One-Shot Kill | All outgoing damage ×9,999 (enemies only) |
| No Cooldown | Skill cooldown stat maximized |
| Infinite Revive | Revival count always 99 |
| Infinite Away | Away-ability count always 99 |
| No Reload | Magazine never runs out; reload is cancelled |
| Rapid Fire | Weapon fire cooldown eliminated |
| No Recoil | Weapon recoil zeroed out |
| Perfect Aim | No bullet spread; aim precision never degrades |

### Action Buttons

| Button | Description |
|--------|-------------|
| Force Level Up | Adds exactly enough XP for one level-up |
| Add 999,999 Gems | Instantly grants 999,999 gems |
| Skull Coins +999 | Instantly grants 999 Skull Coins |
| Full Heal | Restores HP to maximum |
| All Cheats OFF | Resets every toggle to off |

---

## Requirements

- **Bloodshed** — Steam, Windows 64-bit
- **BepInEx 6.x** — IL2CPP build for Windows x64
  → Download from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases) — pick the `win_x64` asset tagged `v6.*`

---

## Installation

### Option A — Pre-built release (recommended)

1. Download the latest **`BloodshedModToolkit_vX.X.X.zip`** from [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases).
2. If BepInEx is not yet installed:
   - Extract the BepInEx zip into your `Bloodshed/` game folder so that `BepInEx/` appears inside it.
   - Launch the game once (a black console window should appear), then quit — this generates the interop assemblies.
3. Extract the release zip. It contains a pre-built `BepInEx/plugins/` structure.
   Merge it into your Bloodshed game folder.
4. Launch Bloodshed. You should see the **★ Cheat Mod** overlay in the top-right corner.

### Option B — Manual DLL placement

Copy `BloodshedModToolkit.dll` to:

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

---

## Usage

| Action | Result |
|--------|--------|
| Click **★ Cheat Mod** overlay (top-right) | Toggle the cheat menu open/closed |
| Click any toggle | Enable / disable that cheat |
| Drag the **Speed** slider | Adjust movement speed multiplier |
| Click **All Cheats OFF** | Disable every active cheat at once |

> The overlay always shows currently active cheats listed in green.

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
# Edit Directory.Build.props — set BepInExPath to your installation

dotnet build -c Release
# Output: bin/Release/net6.0/BloodshedModToolkit.dll
```

Copy the output DLL to `BepInEx/plugins/`.

### CI Setup (GitHub Actions)

CI builds require two GitHub repository secrets:

| Secret | How to create |
|--------|---------------|
| `GAME_LIBS_B64` | Zip the contents of your `BepInEx/interop/` folder after running the game once, then base64-encode the zip: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("interop.zip"))` |

Without `GAME_LIBS_B64`, the build step is skipped and a warning is shown.

---

## Technical Notes

| Cheat | Hook |
|-------|------|
| God Mode | `Health.Damage` Prefix — skips damage for the player |
| Speed Hack | `Q3PlayerController.Accelerate` Prefix — multiplies `targetSpeed` and `accel` |
| One-Shot Kill | `Health.Damage` Prefix — multiplies `damage` ×9,999 for non-player entities |
| Rapid Fire | `ShotAction.SetCooldownEnd` Postfix — resets `CooldownEnd` to 0 |
| No Recoil | `WeaponItem.GetRecoilTotal` Prefix — returns 0 |
| Perfect Aim | `ShotAction.GetSpreadDirection` Prefix — returns `direction.normalized`; `AimPrecisionHandler.ReducePrecision` Prefix — skipped |

---

## ⚠️ Disclaimer

This mod is intended for **offline single-player use only**.
Using it in any online or multiplayer context may violate the game's Terms of Service.
The author is not responsible for any consequences arising from misuse.

---

## License

[MIT](LICENSE)
