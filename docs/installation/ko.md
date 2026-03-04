---
title: "설치 — Bloodshed Mod Toolkit"
description: >-
  Bloodshed Mod Toolkit 설치 방법. BepInEx 6.x IL2CPP be.697 이상 필요.
  빌드된 릴리스 다운로드 또는 DLL 수동 배치 및 소스 빌드 가이드.
lang: ko
---

[← 개요](../overview/ko.md) · [기능](../features/ko.md) · [기술 노트](../technical/ko.md) · [라이선스](../license/ko.md)

**언어:** [English](en.md) · **한국어** · [日本語](ja.md) · [中文](zh-CN.md)

---

# 설치

## 요구사항

- **Bloodshed** — Steam, Windows 64비트
- **BepInEx 6.x (IL2CPP 빌드, Windows x64)**
  최신 `BepInEx_win-x64_*.zip`을 공식 bleeding-edge 빌드 서버에서 다운로드:
  <https://builds.bepinex.dev/projects/bepinex_be>
  IL2CPP 메타데이터 v31을 지원하는 **be.697 이상** 빌드를 사용하세요.

---

## 방법 A — 빌드된 릴리스 설치 (권장)

1. [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)에서 최신 **`BloodshedModToolkit_vX.X.X.zip`** 다운로드.
2. BepInEx가 미설치된 경우:
   - BepInEx zip을 `Bloodshed/` 게임 폴더에 압축 해제합니다 (`BepInEx/` 폴더가 생성됩니다).
   - 게임을 한 번 실행해 인터롭 어셈블리를 생성한 후 종료합니다.
3. 릴리스 ZIP을 압축 해제합니다. 내부에 `BepInEx/plugins/` 구조가 포함되어 있습니다.
   Bloodshed 게임 폴더에 병합합니다.
4. Bloodshed를 실행하고 **F5** 를 눌러 모드 메뉴를 엽니다.

## 방법 B — DLL 수동 배치

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

위 경로에 `BloodshedModToolkit.dll`을 복사합니다.

---

## 소스 빌드

### 필요 환경

- .NET 6 SDK
- BepInEx 6.x 설치 및 게임 최초 실행 완료 (인터롭 어셈블리 생성)

### 빌드 방법

```bash
git clone https://github.com/myso-kr/bloodshed-mod-toolkit.git
cd bloodshed-mod-toolkit

# 설정 파일 복사 후 BepInEx 경로 수정
cp Directory.Build.props.example Directory.Build.props
# Directory.Build.props 에서 BepInExPath 를 실제 경로로 변경

dotnet build -c Release
# 출력: bin/Release/net6.0/BloodshedModToolkit.dll
```

출력된 DLL을 `BepInEx/plugins/`에 복사합니다.

### CI 설정 (GitHub Actions)

| 시크릿 | 생성 방법 |
|--------|-----------|
| `GAME_LIBS_B64` | 게임 최초 실행 후 `BepInEx/interop/` 폴더를 zip으로 압축 후 base64 인코딩: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("interop.zip"))` |

`GAME_LIBS_B64`가 없으면 빌드 단계가 건너뛰어집니다 (스텁 빌드만 실행 — 배포 불가).
