# Bloodshed 作弊模组

> 为 **Bloodshed**（Steam，Windows）添加游戏内作弊菜单的 BepInEx 6.x IL2CPP 插件。

[![Build](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/myso-kr/bloodshed-mod-toolkit)](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Languages / 언어:** [English](README.md) · [한국어](README.ko.md) · [日本語](README.ja.md) · **中文**

---

## 功能

点击屏幕右上角的 **★ Cheat Mod** 悬浮层即可打开/关闭菜单。
菜单会**自动适配游戏当前语言**（支持 19 种语言）。

### 开关

| 作弊 | 说明 |
|------|------|
| 无敌模式 | 玩家不受任何伤害 |
| 无限宝石 | 宝石始终不低于 999,999 |
| 无限骷髅币 | 骷髅币始终不低于 999 |
| 全属性最大化 | 所有玩家属性设为最大值 |
| 移速倍率 | 移动速度倍率调节（滑块：×1 – ×20） |
| 一击必杀 | 对敌人造成的伤害 ×9,999 |
| 冷却消除 | 技能冷却属性最大化 |
| 无限复活 | 复活次数始终保持 99 |
| 无限放逐 | 放逐次数始终保持 99 |
| 无需换弹 | 弹匣永不耗尽，换弹动作被取消 |
| 速射 | 消除武器射击冷却 |
| 无后坐力 | 武器后坐力归零 |
| 完美瞄准 | 无弹药扩散，瞄准精度不下降 |

### 操作按钮

| 按钮 | 说明 |
|------|------|
| 强制升级 | 精确添加升下一级所需的经验值 |
| 获得 999999 宝石 | 立即获得 999,999 宝石 |
| 骷髅币 +999 | 立即获得 999 骷髅币 |
| HP 立即满 | 将 HP 恢复至最大值 |
| 关闭所有作弊 | 一键关闭所有作弊开关 |

---

## 环境需求

- **Bloodshed** — Steam，Windows 64 位
- **BepInEx 6.x** — Windows x64 IL2CPP 版本
  → 从 [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases) 下载标记为 `v6.*` 的 `win_x64` 资源

---

## 安装方法

### 方式 A — 使用预构建版本（推荐）

1. 从 [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases) 下载最新的 **`BloodshedModToolkit_vX.X.X.zip`**。
2. 如果尚未安装 BepInEx：
   - 将 BepInEx 压缩包解压到 `Bloodshed/` 游戏目录下（会生成 `BepInEx/` 文件夹）。
   - 启动游戏一次以生成互操作程序集，然后退出。
3. 解压发布包，其中包含预构建的 `BepInEx/plugins/` 目录结构。
   将其合并到 Bloodshed 游戏目录中即可。
4. 启动 Bloodshed，右上角出现 **★ Cheat Mod** 悬浮层则说明安装成功。

### 方式 B — 手动放置 DLL

将 `BloodshedModToolkit.dll` 复制到：

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

---

## 使用说明

| 操作 | 效果 |
|------|------|
| 点击右上角 **★ Cheat Mod** | 打开/关闭作弊菜单 |
| 点击菜单中的开关 | 启用/禁用对应作弊 |
| 拖动 **速度** 滑块 | 调整移动速度倍率 |
| 点击 **关闭所有作弊** | 一键禁用所有作弊 |

> 悬浮层始终可见，当前激活的作弊会以绿色列出。

---

## 从源码构建

### 前置条件

- .NET 6 SDK
- 已安装 BepInEx 6.x，并至少启动游戏一次（以生成互操作程序集）

### 步骤

```bash
git clone https://github.com/myso-kr/bloodshed-mod-toolkit.git
cd bloodshed-mod-toolkit

# 复制本地配置模板并设置 BepInEx 路径
cp Directory.Build.props.example Directory.Build.props
# 编辑 Directory.Build.props，将 BepInExPath 改为实际路径

dotnet build -c Release
# 输出: bin/Release/net6.0/BloodshedModToolkit.dll
```

将输出的 DLL 复制到 `BepInEx/plugins/`。

---

## ⚠️ 免责声明

本模组仅供**离线单人游戏使用**。
在任何在线或多人游戏环境中使用可能违反游戏服务条款。
因不当使用而产生的任何后果，作者概不负责。

---

## 许可证

[MIT](LICENSE)
