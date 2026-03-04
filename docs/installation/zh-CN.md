[← 概述](../overview/zh-CN.md) · [功能](../features/zh-CN.md) · [技术说明](../technical/zh-CN.md) · [许可证](../license/zh-CN.md)

**语言:** [English](en.md) · [한국어](ko.md) · [日本語](ja.md) · **中文**

---

# 安装方法

## 环境需求

- **Bloodshed** — Steam，Windows 64 位
- **BepInEx 6.x（IL2CPP 版本，Windows x64）**
  从官方 bleeding-edge 构建服务器下载最新 `BepInEx_win-x64_*.zip`：
  <https://builds.bepinex.dev/projects/bepinex_be>
  请使用支持 IL2CPP 元数据 v31 的 **be.697 或更高版本**。

---

## 方式 A — 使用预构建版本（推荐）

1. 从 [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases) 下载最新的 **`BloodshedModToolkit_vX.X.X.zip`**。
2. 如果尚未安装 BepInEx：
   - 将 BepInEx 压缩包解压到 `Bloodshed/` 游戏目录下（会生成 `BepInEx/` 文件夹）。
   - 启动游戏一次以生成互操作程序集，然后退出。
3. 解压发布包，其中包含预构建的 `BepInEx/plugins/` 目录结构。
   将其合并到 Bloodshed 游戏目录中即可。
4. 启动 Bloodshed，按 **F5** 打开 Mod 菜单。

## 方式 B — 手动放置 DLL

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
