---
title: "インストール — Bloodshed Mod Toolkit"
description: >-
  Bloodshed Mod Toolkitのインストール方法。BepInEx 6.x IL2CPP be.697以降が必要。
  ビルド済みリリースまたは手動DLL配置・ソースビルドガイド。
lang: ja
---

[← 概要](../overview/ja.md) · [機能](../features/ja.md) · [技術ノート](../technical/ja.md) · [ライセンス](../license/ja.md)

**言語:** [English](en.md) · [한국어](ko.md) · **日本語** · [中文](zh-CN.md)

---

# インストール

## 必要環境

- **Bloodshed** — Steam、Windows 64ビット
- **BepInEx 6.x（IL2CPP ビルド、Windows x64）**
  最新の `BepInEx_win-x64_*.zip` を公式 bleeding-edge ビルドサーバーからダウンロード:
  <https://builds.bepinex.dev/projects/bepinex_be>
  IL2CPP メタデータ v31 に対応した **be.697 以降** のビルドを使用してください。

---

## 方法A — ビルド済みリリース（推奨）

1. [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases) から最新の **`BloodshedModToolkit_vX.X.X.zip`** をダウンロード。
2. BepInEx が未インストールの場合:
   - BepInEx の zip を `Bloodshed/` ゲームフォルダに展開します（`BepInEx/` フォルダが作成されます）。
   - ゲームを一度起動してインターロップアセンブリを生成後、終了します。
3. リリース zip を展開します。内部に `BepInEx/plugins/` 構成が含まれています。
   Bloodshed ゲームフォルダにマージしてください。
4. Bloodshed を起動し、**F5** でModメニューを開きます。

## 方法B — DLL を手動で配置

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

上記パスに `BloodshedModToolkit.dll` をコピーしてください。

---

## ソースからビルド

### 前提条件

- .NET 6 SDK
- BepInEx 6.x がインストール済みで、ゲームを少なくとも一度起動済み（インターロップアセンブリ生成のため）

### 手順

```bash
git clone https://github.com/myso-kr/bloodshed-mod-toolkit.git
cd bloodshed-mod-toolkit

# ローカル設定ファイルを作成し、BepInEx パスを設定
cp Directory.Build.props.example Directory.Build.props
# Directory.Build.props 内の BepInExPath を実際のパスに変更

dotnet build -c Release
# 出力: bin/Release/net6.0/BloodshedModToolkit.dll
```

出力された DLL を `BepInEx/plugins/` にコピーしてください。

### CI 設定（GitHub Actions）

| シークレット | 作成方法 |
|------------|---------|
| `GAME_LIBS_B64` | ゲーム初回起動後に `BepInEx/interop/` フォルダを zip 化し base64 エンコード: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("interop.zip"))` |

`GAME_LIBS_B64` がない場合、ビルドステップはスキップされます（スタブビルドのみ — デプロイ不可）。
