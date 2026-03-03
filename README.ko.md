# Bloodshed Mod Toolkit

> **Bloodshed** (Steam, Windows)에 치트·밸런스 조정·성능 트윅을 아우르는 BepInEx 6.x IL2CPP 종합 모드 툴킷.

[![Build](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/myso-kr/bloodshed-mod-toolkit/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/myso-kr/bloodshed-mod-toolkit)](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Languages / 언어:** [English](README.md) · **한국어** · [日本語](README.ja.md) · [中文](README.zh-CN.md)

---

## 기능

화면 **우측 상단 ★ Mod Toolkit** 오버레이를 클릭하면 메뉴를 열고 닫을 수 있습니다.
메뉴는 **게임 내 언어 설정을 자동으로 반영**합니다 (19개 언어 지원).

### 토글

| 기능 | 설명 |
|------|------|
| 무적 모드 | 플레이어가 피해를 입지 않습니다 |
| 무한 젬 | 젬이 999,999 아래로 내려가지 않습니다 |
| 무한 골든 스컬 | 골든 스컬이 999 아래로 내려가지 않습니다 |
| 전체 스탯 최대화 | 모든 플레이어 스탯을 최대로 설정합니다 |
| 이동속도 배율 | 이동속도 배율 조절 (슬라이더: ×1 – ×20) |
| 원샷킬 | 적 대상 피해 ×9,999 |
| 쿨다운 제거 | 스킬 쿨다운 스탯을 최대로 설정합니다 |
| 무한 부활 | 부활 횟수를 항상 99로 유지합니다 |
| 무한 추방기 | 추방기 횟수를 항상 99로 유지합니다 |
| 장전 없음 | 탄창이 소모되지 않으며 장전이 취소됩니다 |
| 속사 | 무기 발사 쿨다운을 제거합니다 |
| 무반동 | 무기 반동을 0으로 만듭니다 |
| 완벽한 조준 | 산탄 없음, 정밀도 감소 없음 |

### 버튼

| 버튼 | 설명 |
|------|------|
| 레벨업 강제 | 다음 레벨업에 필요한 정확한 XP를 추가합니다 |
| 젬 999999 즉시 지급 | 젬 999,999를 즉시 지급합니다 |
| 골든 스컬 +999 | 골든 스컬 999를 즉시 지급합니다 |
| HP 즉시 풀충전 | HP를 최대로 회복합니다 |
| 모든 치트 OFF | 모든 토글을 비활성화합니다 |

---

## 요구사항

- **Bloodshed** — Steam, Windows 64비트
- **BepInEx 6.x** — Windows x64 IL2CPP 빌드
  → [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases)에서 `v6.*` 태그의 `win_x64` 에셋을 다운로드

---

## 설치

### 방법 A — 빌드된 릴리스 설치 (권장)

1. [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)에서 최신 **`BloodshedModToolkit_vX.X.X.zip`** 다운로드.
2. BepInEx가 설치되지 않은 경우:
   - BepInEx zip을 `Bloodshed/` 게임 폴더에 압축 해제합니다 (안에 `BepInEx/` 폴더가 생깁니다).
   - 게임을 한 번 실행해 인터롭 어셈블리를 생성한 후 종료합니다.
3. 릴리스 ZIP을 압축 해제합니다. 내부에 `BepInEx/plugins/` 구조가 포함되어 있습니다.
   Bloodshed 게임 폴더에 병합하면 됩니다.
4. Bloodshed를 실행합니다. 우측 상단에 **★ Mod Toolkit** 오버레이가 표시되면 설치 성공입니다.

### 방법 B — DLL 수동 배치

```
Bloodshed/BepInEx/plugins/BloodshedModToolkit.dll
```

위 경로에 `BloodshedModToolkit.dll`을 복사합니다.

---

## 사용법

| 동작 | 결과 |
|------|------|
| 우측 상단 **★ Mod Toolkit** 클릭 | 모드 메뉴 열기/닫기 |
| 메뉴 내 토글 클릭 | 해당 기능 활성화/비활성화 |
| **속도** 슬라이더 조절 | 이동속도 배율 변경 |
| **모든 치트 OFF** 클릭 | 모든 기능 한 번에 초기화 |

> 오버레이는 항상 표시되며, 현재 활성화된 기능 목록을 초록색으로 나열합니다.

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

CI 빌드에는 GitHub 저장소 시크릿이 필요합니다:

| 시크릿 | 생성 방법 |
|--------|-----------|
| `GAME_LIBS_B64` | 게임 최초 실행 후 `BepInEx/interop/` 폴더를 zip으로 묶은 뒤 base64로 인코딩: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("interop.zip"))` |

`GAME_LIBS_B64`가 설정되지 않으면 빌드 단계가 건너뛰어지고 경고가 표시됩니다.

---

## 기술 노트

| 치트 | 후킹 방식 |
|------|-----------|
| 무적 모드 | `Health.Damage` Prefix — 플레이어 대상 피해 차단 |
| 이동속도 | `Q3PlayerController.Accelerate` Prefix — `targetSpeed`, `accel` 배율 적용 |
| 원샷킬 | `Health.Damage` Prefix — 비플레이어 대상 피해 ×9,999 |
| 속사 | `ShotAction.SetCooldownEnd` Postfix — `CooldownEnd`를 0으로 강제 |
| 무반동 | `WeaponItem.GetRecoilTotal` Prefix — 0 반환 |
| 완벽한 조준 | `ShotAction.GetSpreadDirection` Prefix — `direction.normalized` 반환; `AimPrecisionHandler.ReducePrecision` Prefix — 스킵 |

---

## ⚠️ 주의사항

이 모드는 **오프라인 싱글플레이 전용**으로 제작되었습니다.
온라인 또는 멀티플레이 환경에서 사용할 경우 게임 이용약관에 위반될 수 있습니다.
부적절한 사용으로 인한 결과에 대해 제작자는 책임을 지지 않습니다.

---

## 라이선스

[MIT](LICENSE)
