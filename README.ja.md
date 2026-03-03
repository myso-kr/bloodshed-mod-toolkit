# Bloodshed Mod Toolkit

> **Bloodshed** (Steam, Windows) にチート・バランス調整・パフォーマンスチューニングを網羅する BepInEx 6.x IL2CPP 総合Modツールキット。

[![Build](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/myso-kr/bloodshed-mod-toolkit)](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Languages / 언어:** [English](README.md) · [한국어](README.ko.md) · **日本語** · [中文](README.zh-CN.md)

---

## 機能

画面右上の **★ Mod Toolkit** オーバーレイをクリックするとModメニューが開きます。
メニューは**ゲーム内の言語設定に自動で対応**します（19言語サポート）。

### トグル

| チート | 説明 |
|--------|------|
| 無敵モード | プレイヤーがダメージを受けません |
| ジェム無限 | ジェムが 999,999 を下回りません |
| スカルコイン無限 | スカルコインが 999 を下回りません |
| 全ステータス最大化 | すべてのプレイヤーステータスを最大に設定します |
| 移動速度倍率 | 移動速度の倍率を調整します（スライダー: ×1 – ×20） |
| 一撃必殺 | 敵へのダメージが ×9,999 になります |
| クールダウン除去 | スキルのクールダウンスタットを最大化します |
| 無限復活 | 復活回数を常に 99 に維持します |
| 無限追放 | 追放回数を常に 99 に維持します |
| リロードなし | 弾薬が消費されず、リロードがキャンセルされます |
| 速射 | 武器の発射クールダウンをなくします |
| 無反動 | 武器の反動をゼロにします |
| 完璧な照準 | 弾の拡散なし・照準精度の劣化なし |

### アクションボタン

| ボタン | 説明 |
|--------|------|
| 強制レベルアップ | 次のレベルアップに必要な経験値を正確に追加します |
| ジェム 999999 獲得 | ジェムを 999,999 即時付与します |
| スカルコイン +999 | スカルコインを 999 即時付与します |
| HP全回復 | HP を最大まで回復します |
| 全チートOFF | すべてのトグルをオフにします |

---

## 必要環境

- **Bloodshed** — Steam, Windows 64ビット
- **BepInEx 6.x** — Windows x64 IL2CPP ビルド
  → [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases) から `v6.*` タグの `win_x64` アセットをダウンロード

---

## インストール

### 方法A — ビルド済みリリース（推奨）

1. [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases) から最新の **`BloodshedModToolkit_vX.X.X.zip`** をダウンロード。
2. BepInEx が未インストールの場合:
   - BepInEx の zip を `Bloodshed/` ゲームフォルダに展開します（`BepInEx/` フォルダが作成されます）。
   - ゲームを一度起動してインターロップアセンブリを生成後、終了します。
3. リリース zip を展開します。内部に `BepInEx/plugins/` 構成が含まれています。
   Bloodshed ゲームフォルダにマージしてください。
4. Bloodshed を起動します。右上に **★ Mod Toolkit** オーバーレイが表示されれば成功です。

### 方法B — DLL を手動で配置

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

上記パスに `BloodshedModToolkit.dll` をコピーしてください。

---

## 使い方

| 操作 | 結果 |
|------|------|
| 右上の **★ Mod Toolkit** をクリック | Modメニューの開閉 |
| メニュー内のトグルをクリック | 機能の有効化/無効化 |
| **Speed** スライダーをドラッグ | 移動速度倍率の調整 |
| **全チートOFF** をクリック | すべての機能を一括リセット |

> オーバーレイは常に表示され、現在有効な機能が緑色でリスト表示されます。

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

---

## ⚠️ 免責事項

このModは**オフラインのシングルプレイ専用**として作成されています。
オンラインまたはマルチプレイ環境での使用はゲームの利用規約に違反する可能性があります。
不正使用による結果について、作者は一切の責任を負いません。

---

## ライセンス

[MIT](LICENSE)
