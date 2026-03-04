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

**Languages:** [English](README.md) · **한국어** · [日本語](README.ja.md) · [中文](README.zh-CN.md)

---

![banner](docs/images/banner_hero.png)

게임을 켠다. 컷신도 없고 튜토리얼 팝업도 없다. 음악이 터진다 — 거칠고 묵직한 메탈 리프 — 그리고 바로 전투다.
픽셀 아트 악마들이 화면을 가득 채운다. 산탄총이 반동을 일으킨다. 뭔가가 만족스러운 붉은 폭발로 터진다. 12초 만에 죽는다.
사망 화면이 끝나기도 전에 재시작을 누른다.

*그게 Bloodshed다.* DOOM의 DNA를 로그라이크 렌즈로 걸러낸 게임 — 빠르고, 시끄럽고, 철저하게 가혹하다.

개발사는 정식 업데이트를 마무리했지만, 플레이어들은 떠나지 않았다.
**Steam 긍정 평가 85%.** 미션 하나를 더 돌고, 캐릭터 하나를 더 해금하고, 브로큰 시너지를 하나 더 찾아내는 작고 끈질긴 커뮤니티.

이 툴킷은 코-업이 하고 싶어서 시작됐다. 그다음엔 트윅이 필요했고, 그다음엔 스폰 수를 ×4로 올리고 무적을 켜면 어떻게 되는지 보고 싶었다. 지금은 그걸 전부 한다 — F5 하나로.

---

## 개요

게임 중 **F5** 를 눌러 모드 메뉴를 열고 닫습니다.
메뉴는 **게임 내 언어 설정을 자동으로 반영**합니다.

메뉴는 4개의 탭으로 구성됩니다:

| 탭 | 내용 |
|----|------|
| **CHEATS** | 생존/경제/전투/이동 토글 및 액션 버튼 |
| **TWEAKS** | 난이도 프리셋 및 세부 밸런스 슬라이더 |
| **CO-OP** | Steam P2P 로비 — 호스트/참가/친구 목록/XP 공유/미션 게이트 |
| **BOTS** | AI 봇 동반자 (1–3명) |

---

## 기능

![banner](docs/images/banner_power.png)

### CHEATS 탭

#### 생존

| 토글 | 효과 |
|------|------|
| 무적 모드 | 플레이어가 피해를 입지 않습니다 |
| 전체 스탯 최대화 | 매 프레임 HP를 99,999로 회복합니다 |
| 무한 부활 | 부활 횟수를 항상 99로 유지합니다 |
| 무한 추방기 | 추방기 횟수를 항상 99로 유지합니다 |

#### 경제

| 토글 | 효과 |
|------|------|
| 무한 젬 | 젬이 999,999 아래로 내려가지 않습니다 |
| 무한 골든 스컬 | 골든 스컬이 999 아래로 내려가지 않습니다 |

#### 전투

| 토글 | 효과 |
|------|------|
| 원샷킬 | 적 대상 피해 ×9,999 |
| 쿨다운 제거 | 스킬 쿨다운 스탯을 최대화합니다 |
| 속사 | 무기 발사 쿨다운을 제거합니다 |
| 무반동 | 무기 반동을 0으로 만듭니다 |
| 완벽한 조준 | 산탄 없음, 정밀도 감소 없음 |
| 장전 없음 | 탄창이 소모되지 않으며 장전이 취소됩니다 |

#### 이동

| 토글 | 효과 |
|------|------|
| 이동속도 배율 | 이동속도 배율 조절 (슬라이더: ×1.0 – ×20.0) |

#### 액션 버튼

| 버튼 | 효과 |
|------|------|
| 레벨업 강제 | 다음 레벨업에 필요한 XP를 정확히 추가합니다 |
| 젬 999,999 즉시 지급 | 젬 999,999를 즉시 지급합니다 |
| 골든 스컬 +999 | 골든 스컬 999를 즉시 지급합니다 |
| HP 풀충전 | HP를 최대로 회복합니다 |
| 모든 치트 OFF | 모든 토글을 비활성화합니다 |

#### 오버레이 위치

상태 패널과 DPS 패널을 **좌상단**, **중앙 상단**, **우상단**에 고정하거나 숨길 수 있습니다.

---

### TWEAKS 탭

**프리셋** 버튼 하나로 난이도를 바꾸거나, 슬라이더로 각 수치를 세밀하게 조정합니다.

#### 프리셋

| 프리셋 | 설명 |
|--------|------|
| **Mortal** | 쉬움 — 플레이어 강화, 에너미 약화, 스폰 감소 |
| **Hunter** | 기본 — 모든 값 ×1.00 (게임 기본값) |
| **Slayer** | 어려움 — 에너미 강화, 스폰 +50% |
| **Demon** | 매우 어려움 — 에너미 피해 ×2, 체력 ×2.5, 스폰 ×2 |
| **Apocalypse** | 극한 — 에너미 피해 ×3, 체력 ×4, 스폰 ×3 |

#### 개별 슬라이더

| 분류 | 항목 | 범위 |
|------|------|------|
| 플레이어 | HP 배율 | ×0.10 – ×4.00 |
| 플레이어 | 이동속도 배율 | ×0.50 – ×3.00 |
| 무기 | 피해 배율 | ×0.50 – ×3.00 |
| 무기 | 발사속도 배율 | ×0.50 – ×3.00 |
| 무기 | 장전속도 배율 | ×0.50 – ×3.00 |
| 에너미 | HP 배율 | ×0.25 – ×5.00 |
| 에너미 | 이동속도 배율 | ×0.25 – ×3.00 |
| 에너미 | 피해 배율 | ×0.25 – ×5.00 |
| 스폰 | 수량 배율 | ×0.25 – ×4.00 |

---

![banner](docs/images/banner_coop.png)

### CO-OP 탭

Steam 로비 기반 **P2P 코-업**을 최대 4인까지 지원합니다.
참여하는 모든 플레이어가 모드를 설치해야 합니다.

#### 플레이 방법

1. **호스트** — CO-OP 탭에서 **로비 생성** 클릭.
2. **게스트** — 호스트에게 로비 ID를 받아 Join 필드에 붙여넣고 **참가** 클릭.
   또는 **Friends** 섹션에서 친구 목록을 새로고침하여 직접 참가하거나 초대합니다.

#### XP 공유 모드

| 모드 | 동작 |
|------|------|
| 독립 | 각 플레이어가 XP를 독자적으로 획득합니다 |
| 복제 | 게스트가 호스트와 동일한 XP를 받습니다 (기본값) |
| 분할 | 호스트 XP의 절반이 게스트에게 전달됩니다 |

#### 미션 게이트

호스트가 미션에 진입하면 게스트는 호스트 신호를 받을 때까지 로딩 화면에서 대기합니다.
씬 독립 진입으로 인한 동기화 오류를 방지합니다.

---

### BOTS 탭

**AI 봇 동반자 1–3명**을 소환합니다. 봇의 레벨, HP, 위치가 실시간으로 표시됩니다.

---

### 단축키

| 키 | 동작 |
|----|------|
| **F5** | 모드 메뉴 열기/닫기 |
| **F6** | HP 풀충전 |
| **F7** | 레벨업 강제 |

---

## 요구사항

- **Bloodshed** — Steam, Windows 64비트
- **BepInEx 6.x (IL2CPP 빌드, Windows x64)**
  최신 `BepInEx_win-x64_*.zip`을 공식 bleeding-edge 빌드 서버에서 다운로드:
  <https://builds.bepinex.dev/projects/bepinex_be>
  IL2CPP 메타데이터 v31을 지원하는 **be.697 이상** 빌드를 사용하세요.

---

## 설치

### 방법 A — 빌드된 릴리스 설치 (권장)

1. [Releases](https://github.com/myso-kr/bloodshed-mod-toolkit/releases)에서 최신 **`BloodshedModToolkit_vX.X.X.zip`** 다운로드.
2. BepInEx가 미설치된 경우:
   - BepInEx zip을 `Bloodshed/` 게임 폴더에 압축 해제합니다 (`BepInEx/` 폴더가 생성됩니다).
   - 게임을 한 번 실행해 인터롭 어셈블리를 생성한 후 종료합니다.
3. 릴리스 ZIP을 압축 해제합니다. 내부에 `BepInEx/plugins/` 구조가 포함되어 있습니다.
   Bloodshed 게임 폴더에 병합합니다.
4. Bloodshed를 실행하고 **F5** 를 눌러 모드 메뉴를 엽니다.

### 방법 B — DLL 수동 배치

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

---

## 기술 노트

| 기능 | 후킹 방식 |
|------|-----------|
| 무적 모드 | `PlayerStats.TakeDamage(float, GameObject)` Prefix → 스킵 |
| 무한 젬 | `PlayerStats.SetMoney(float)` Postfix + `PersistentData.currentMoney` 매 프레임 |
| 이동속도 배율 | `Q3PlayerController.GetPlayerSpeed/ForwardSpeed/StrafeSpeed` Postfix → 배율 적용 |
| 원샷킬 | `Health.Damage(float, …)` Prefix → 비플레이어 대상 ×9,999 |
| 쿨다운 제거 | `ActiveAbilityHandler.ProcessActiveAbilities()` Postfix → 타이머 0 |
| 장전 없음 | `WeaponItem.GetCurrentAmmo` Prefix → 최대값 반환 |
| 속사 | `ShotAction.SetCooldownEnd` Postfix → 0 강제 |
| 무반동 | `WeaponItem.GetRecoilTotal` Prefix → 0 반환 |
| 완벽한 조준 | `ShotAction.GetSpreadDirection` Prefix → normalized 반환; `AimPrecisionHandler.ReducePrecision` Prefix → 스킵 |
| 에너미 속도 트윅 | `EnemyAbilityController.SetBehaviorWalkable(float)` Postfix → 배율 적용 |
| 스폰 수량 트윅 | `SpawnProcessor.GetMaxEnemyCount()` Postfix + `SpawnDirector.SpawnEnemies(…)` Prefix → 배율 적용 |
| 플레이어 HP 트윅 | `PlayerStats.RecalculateStats()` Postfix → MaxHp 배율 적용 |

---

## ⚠️ 주의사항

코-업 기능은 **Steam을 통해 직접 초대한 친구**와만 연결됩니다.
무적, 원샷킬 등의 치트는 **모든 참가자가 동의한 비공개 세션**에서만 사용하세요.
공개 로비 등 다른 플레이어에게 영향을 미치는 방식으로 치트를 사용하면 게임 이용약관에 위반될 수 있습니다.
부적절한 사용으로 인한 결과에 대해 제작자는 책임을 지지 않습니다.

---

## 라이선스

[MIT](LICENSE)

---

> Bloodshed를 아직 플레이해본 적 없다면 — 먼저 그걸 하세요.

[![Steam](https://img.shields.io/badge/Steam-Bloodshed-1b2838?logo=steam)](https://store.steampowered.com/app/2747550/Bloodshed/)
