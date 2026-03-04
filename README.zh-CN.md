# Bloodshed Mod Toolkit

[![Bloodshed on Steam](docs/images/steam_card.svg)](https://store.steampowered.com/app/2747550/Bloodshed/)

[![Latest Release](https://img.shields.io/github/v/release/myso-kr/bloodshed-mod-toolkit?color=dc2626&logo=github&logoColor=white)](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-22c55e)](LICENSE)
![Platform](https://img.shields.io/badge/Windows-x64-0078D6?logo=windows&logoColor=white)
[![BepInEx](https://img.shields.io/badge/BepInEx-6.x_IL2CPP-7c3aed?logo=unity&logoColor=white)](https://builds.bepinex.dev/projects/bepinex_be)
![CO-OP](https://img.shields.io/badge/CO--OP-4_Players-1b2838?logo=steam&logoColor=white)
![God Mode](https://img.shields.io/badge/God_Mode-ON-dc2626)
![Tweaks](https://img.shields.io/badge/Tweaks-9_Sliders-ea580c)
![AI Bots](https://img.shields.io/badge/AI_Bots-up_to_3-52525b)
![i18n](https://img.shields.io/badge/i18n-EN_KO_JA_ZH-2563eb)

**Languages:** [English](README.md) · [한국어](README.ko.md) · [日本語](README.ja.md) · **中文**

---

![banner](docs/images/banner_hero.png)

启动游戏。没有过场动画，没有教程弹窗。音乐响起 — 那段粗粝、有力的金属即兴重复段 — 然后直接开打。
像素风恶魔涌满屏幕。霰弹枪踢出后坐力。某个东西在令人满足的红色爆炸中碎裂。十二秒后死亡。
死亡画面还没结束，手指已经按下了重试。

*这就是 Bloodshed。* DOOM 的 DNA 经过 Roguelike 镜头过滤 — 快速、嘈杂、毫不留情。

开发者已停止主动开发，但玩家没有离开。
**Steam 好评率 85%。** 一个小而倔强的社区，不断地再跑一次任务，再解锁一个角色，再找一个破坏性的协同效果。

这个工具集起源于想玩联机。然后需要调整数值。然后想看看把刷怪数量拉到 ×4 同时开无敌会发生什么。现在它全都能做 — 按一下 F5 就行。

---

## 概述

游戏中按 **F5** 打开或关闭 Mod 菜单。
菜单会**自动适配游戏当前语言设置**。

菜单包含四个标签页：

| 标签 | 内容 |
|------|------|
| **CHEATS** | 生存/经济/战斗/移动开关及操作按钮 |
| **TWEAKS** | 难度预设与细粒度平衡滑块 |
| **CO-OP** | Steam P2P 房间 — 创建/加入/好友列表/经验共享/任务门禁 |
| **BOTS** | AI 机器人伙伴（1–3 个） |

---

## 功能

![banner](docs/images/banner_power.png)

### CHEATS 标签

#### 生存

| 开关 | 效果 |
|------|------|
| 无敌模式 | 玩家不受任何伤害 |
| 全属性最大化 | 每帧将 HP 恢复至 99,999 |
| 无限复活 | 复活次数始终保持 99 |
| 无限放逐 | 放逐次数始终保持 99 |

#### 经济

| 开关 | 效果 |
|------|------|
| 无限宝石 | 宝石始终不低于 999,999 |
| 无限骷髅币 | 骷髅币始终不低于 999 |

#### 战斗

| 开关 | 效果 |
|------|------|
| 一击必杀 | 对敌人造成的伤害 ×9,999 |
| 冷却消除 | 技能冷却属性最大化 |
| 速射 | 消除武器射击冷却 |
| 无后坐力 | 武器后坐力归零 |
| 完美瞄准 | 无弹药扩散，瞄准精度不下降 |
| 无需换弹 | 弹匣永不耗尽，换弹动作被取消 |

#### 移动

| 开关 | 效果 |
|------|------|
| 移速倍率 | 移动速度倍率调节（滑块：×1.0 – ×20.0） |

#### 操作按钮

| 按钮 | 效果 |
|------|------|
| 强制升级 | 精确添加升下一级所需的经验值 |
| 获得 999,999 宝石 | 立即获得 999,999 宝石 |
| 骷髅币 +999 | 立即获得 999 骷髅币 |
| HP 立即满 | 将 HP 恢复至最大值 |
| 关闭所有作弊 | 一键关闭所有开关 |

#### 悬浮层位置

状态面板与 DPS 面板可固定于**左上**、**中上**、**右上**，或完全隐藏。

---

### TWEAKS 标签

一键应用**难度预设**，或通过滑块精细调整每个数值。

#### 预设

| 预设 | 说明 |
|------|------|
| **Mortal** | 简单 — 强化玩家、削弱敌人、减少刷怪 |
| **Hunter** | 默认 — 所有值 ×1.00（游戏原始值） |
| **Slayer** | 困难 — 敌人增强，刷怪量 +50% |
| **Demon** | 极难 — 敌人伤害 ×2，HP ×2.5，刷怪 ×2 |
| **Apocalypse** | 极限 — 敌人伤害 ×3，HP ×4，刷怪 ×3 |

#### 独立滑块

| 分类 | 参数 | 范围 |
|------|------|------|
| 玩家 | HP 倍率 | ×0.10 – ×4.00 |
| 玩家 | 移速倍率 | ×0.50 – ×3.00 |
| 武器 | 伤害倍率 | ×0.50 – ×3.00 |
| 武器 | 射速倍率 | ×0.50 – ×3.00 |
| 武器 | 换弹速度倍率 | ×0.50 – ×3.00 |
| 敌人 | HP 倍率 | ×0.25 – ×5.00 |
| 敌人 | 移速倍率 | ×0.25 – ×3.00 |
| 敌人 | 伤害倍率 | ×0.25 – ×5.00 |
| 刷怪 | 数量倍率 | ×0.25 – ×4.00 |

---

![banner](docs/images/banner_coop.png)

### CO-OP 标签

基于 Steam 房间的 **P2P 联机**，最多支持 4 名玩家。
所有参与玩家均需安装本 Mod。

#### 游玩方法

1. **房主** — 在 CO-OP 标签中点击**创建房间**。
2. **访客** — 从房主处获取房间 ID，粘贴到 Join 输入框后点击**加入**。
   也可在 **Friends** 板块刷新好友列表，直接加入或发送邀请。

#### 经验共享模式

| 模式 | 行为 |
|------|------|
| 独立 | 每位玩家独立获得经验 |
| 复制 | 访客获得与房主相同的经验（默认） |
| 分割 | 房主经验减半后传递给访客 |

#### 任务门禁

房主进入任务时，访客将在加载界面等待，直至收到房主信号，
防止独立进场导致的同步错误。

---

### BOTS 标签

召唤 **1–3 个 AI 机器人伙伴**，实时显示其等级、HP 与位置信息。

---

### 快捷键

| 按键 | 功能 |
|------|------|
| **F5** | 打开 / 关闭 Mod 菜单 |
| **F6** | HP 立即满 |
| **F7** | 强制升级 |

---

## 环境需求

- **Bloodshed** — Steam，Windows 64 位
- **BepInEx 6.x（IL2CPP 版本，Windows x64）**
  从官方 bleeding-edge 构建服务器下载最新 `BepInEx_win-x64_*.zip`：
  <https://builds.bepinex.dev/projects/bepinex_be>
  请使用支持 IL2CPP 元数据 v31 的 **be.697 或更高版本**。

---

## 安装方法

### 方式 A — 使用预构建版本（推荐）

1. 从 [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases) 下载最新的 **`BloodshedModToolkit_vX.X.X.zip`**。
2. 如果尚未安装 BepInEx：
   - 将 BepInEx 压缩包解压到 `Bloodshed/` 游戏目录下（会生成 `BepInEx/` 文件夹）。
   - 启动游戏一次以生成互操作程序集，然后退出。
3. 解压发布包，其中包含预构建的 `BepInEx/plugins/` 目录结构。
   将其合并到 Bloodshed 游戏目录中即可。
4. 启动 Bloodshed，按 **F5** 打开 Mod 菜单。

### 方式 B — 手动放置 DLL

将 `BloodshedModToolkit.dll` 复制到：

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

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

### CI 配置（GitHub Actions）

| 密钥 | 创建方法 |
|------|---------|
| `GAME_LIBS_B64` | 游戏首次运行后，将 `BepInEx/interop/` 文件夹压缩为 zip 并 base64 编码：`[Convert]::ToBase64String([IO.File]::ReadAllBytes("interop.zip"))` |

缺少 `GAME_LIBS_B64` 时构建步骤将被跳过（仅执行 stub 构建 — 无法部署）。

---

## 技术说明

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

---

## 许可证

[MIT](LICENSE)

---

> 如果你还没玩过 Bloodshed — 先去玩。

[![Steam](https://img.shields.io/badge/Steam-Bloodshed-1b2838?logo=steam)](https://store.steampowered.com/app/2747550/Bloodshed/)
