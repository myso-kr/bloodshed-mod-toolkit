---
title: "Installation — Bloodshed Mod Toolkit"
description: >-
  Install Bloodshed Mod Toolkit: requires BepInEx 6.x IL2CPP be.697+.
  Pre-built release download or manual DLL placement and build-from-source guide.
lang: en
---

# Installation

## Requirements

- **Bloodshed** — Steam, Windows 64-bit
- **BepInEx 6.x (IL2CPP build, Windows x64)**
  Download the latest `BepInEx_win-x64_*.zip` from the bleeding-edge build server:
  <https://builds.bepinex.dev/projects/bepinex_be>
  Use build **be.697 or later** (required for IL2CPP metadata v31).

---

## Option A — Pre-built release (recommended)

1. Download the latest **`BloodshedModToolkit_vX.X.X.zip`** from [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases).
2. If BepInEx is not yet installed:
   - Extract the BepInEx zip into your `Bloodshed/` game folder so that `BepInEx/` appears at the root.
   - Launch the game once (a black console window should appear briefly), then quit — this generates the interop assemblies.
3. Extract the release zip. It contains a pre-built `BepInEx/plugins/` directory.
   Merge it into your Bloodshed game folder.
4. Launch Bloodshed. Press **F5** to open the mod menu.

## Option B — Manual DLL placement

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
