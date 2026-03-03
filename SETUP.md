# BloodshedModToolkit 설치 및 설정 가이드

## 0. 필요 도구

| 도구 | 다운로드 |
|------|---------|
| BepInEx 6.x (IL2CPP win-x64) | https://github.com/BepInEx/BepInEx/releases (BepInEx_win-x64_*.zip) |
| Il2CppDumper | https://github.com/Perfare/Il2CppDumper/releases |
| .NET 6 SDK 이상 | https://dotnet.microsoft.com/download |

---

## 1. BepInEx 설치

1. `BepInEx_win-x64_*.zip` 을 `D:\SteamLibrary\steamapps\common\Bloodshed\` 에 압축 해제
2. 구조 확인:
   ```
   Bloodshed/
   ├── BepInEx/
   │   ├── core/
   │   └── plugins/      ← 모드 DLL 배치 위치
   ├── winhttp.dll       ← BepInEx 프록시 (있어야 함)
   └── Bloodshed.exe
   ```
3. **게임 한 번 실행** → BepInEx 가 `BepInEx/interop/` 에 Il2Cpp 래퍼 어셈블리 자동 생성

---

## 2. Il2CppDumper 실행 (클래스명 확인용)

```cmd
Il2CppDumper.exe ^
  "D:\SteamLibrary\steamapps\common\Bloodshed\GameAssembly.dll" ^
  "D:\SteamLibrary\steamapps\common\Bloodshed\Bloodshed_Data\il2cpp_data\Metadata\global-metadata.dat" ^
  "output\"
```

`output\dump.cs` 에서 아래 키워드 검색:

| 검색어 | 목적 파일 |
|--------|----------|
| `TakeDamage`, `ApplyDamage` | Cheats/GodMode.cs |
| `HasEnough`, `SpendGems`, `Gems` | Cheats/InfiniteGems.cs |
| `GetStat`, `RefreshStat`, `MaxHp`, `Might` | Cheats/StatMaxer.cs |
| `GetMoveSpeed`, `moveSpeed`, `Agility` | Cheats/SpeedHack.cs |
| `IsOnCooldown`, `StartCooldown` | Cheats/CooldownRemover.cs |

---

## 3. 코드 수정 (TODO 교체)

각 Cheats/*.cs 파일의 `[HarmonyTargetMethod]` 블록을 삭제하고
`[HarmonyPatch(typeof(실제클래스), "실제메서드")]` 어트리뷰트를 활성화합니다.

### 예시 (GodMode.cs)
```csharp
// 수정 전
[HarmonyPatch]
public static class GodModePatch
{
    [HarmonyTargetMethod]
    static MethodBase? TargetMethod() { return null; }
    ...
}

// 수정 후 (dump.cs 에서 확인한 실제명 사용)
[HarmonyPatch(typeof(PlayerHealth), "TakeDamage")]
public static class GodModePatch
{
    // TargetMethod 블록 삭제
    static bool Prefix() => !CheatState.GodMode;
}
```

---

## 4. 빌드 및 배포

```cmd
cd BloodshedModToolkit
build.bat
```

또는 수동:
```cmd
dotnet build -c Release
copy bin\Release\net6.0\BloodshedModToolkit.dll ..\BepInEx\plugins\
```

---

## 5. 게임 실행 및 검증

1. Bloodshed.exe 실행
2. BepInEx 콘솔(검은 창)에서 확인:
   ```
   [Info  :Bloodshed Cheat Mod] Bloodshed Cheat Mod v1.0.0 loaded successfully!
   ```
3. 게임 내 `INSERT` 키 → 치트 메뉴 열림
4. F1–F7 단축키 또는 메뉴 토글로 치트 활성화

---

## 6. 치트 목록

| 단축키 | 기능 |
|--------|------|
| INSERT | 치트 메뉴 열기/닫기 |
| F1 | God Mode — 피해 0 |
| F2 | Infinite Gems — 젬 무한 |
| F3 | Max Stats — 전체 스탯 9999 |
| F4 | Speed Hack — 이동속도 배율 (슬라이더로 조절) |
| F5 | One-Shot Kill — 공격력 999× |
| F6 | No Cooldown — 쿨다운 즉시 초기화 |
| F7 | Infinite Revive — 부활 횟수 무제한 |

---

## 주의사항

- BepInEx **6.x (IL2CPP 빌드)** 만 사용. BepInEx 5.x 는 Mono 전용이라 동작 안 함.
- interop 어셈블리는 게임 실행 후에만 생성됨 → 빌드 전에 게임을 먼저 실행할 것.
- 클래스/메서드명은 Il2CppDumper 결과를 반드시 확인한 후 교체할 것.
