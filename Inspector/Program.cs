/// Balance Tweak Inspector — 밸런스 관련 모든 게임 클래스 전수 조사
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

var interopDir = @"D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx\interop";
var coreDir    = @"D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx\core";

var allDlls = Directory.GetFiles(interopDir, "*.dll")
    .Concat(Directory.GetFiles(coreDir, "*.dll"))
    .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
    .ToArray();

using var mlc = new MetadataLoadContext(new PathAssemblyResolver(allDlls));
var asm   = mlc.LoadFromAssemblyPath(Path.Combine(interopDir, "Assembly-CSharp.dll"));
var types = asm.GetTypes();

// ─── 헬퍼 ────────────────────────────────────────────────────────────────────
static void DumpType(Type t, string header, Regex? fieldFilter = null, Regex? methodFilter = null)
{
    Console.WriteLine($"\n{'=',1}== {header} ({t.FullName}) ===");

    // 프로퍼티
    var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(p => p.DeclaringType?.FullName == t.FullName
                 && !p.Name.StartsWith("Native") && p.Name is not ("Pointer" or "ObjectClass"))
        .OrderBy(p => p.Name).ToArray();
    foreach (var p in props)
        Console.WriteLine($"  prop   {p.PropertyType.Name,-20} {p.Name}");

    // 필드
    var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(f => f.DeclaringType?.FullName == t.FullName
                 && !f.Name.StartsWith("<") && !f.Name.StartsWith("Native"))
        .Where(f => fieldFilter == null || fieldFilter.IsMatch(f.Name))
        .OrderBy(f => f.Name).ToArray();
    foreach (var f in fields)
        Console.WriteLine($"  field  {f.FieldType.Name,-20} {f.Name}");

    // 메서드
    var meths = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(m => m.DeclaringType?.FullName == t.FullName && !m.IsSpecialName)
        .Where(m => methodFilter == null || methodFilter.IsMatch(m.Name))
        .OrderBy(m => m.Name).ToArray();
    foreach (var m in meths)
    {
        try
        {
            var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            Console.WriteLine($"  meth   {m.ReturnType.Name,-20} {m.Name}({parms})");
        }
        catch { }
    }
}

static void SearchTypes(Type[] types, string sectionName, Regex nameRx)
{
    Console.WriteLine($"\n\n{'#',1}## {sectionName} — 키워드 매칭 타입 목록 ###");
    foreach (var t in types.Where(t => nameRx.IsMatch(t.Name)).OrderBy(t => t.Name))
        Console.WriteLine($"  {t.FullName}");
}

static void SearchMembers(Type[] types, string sectionName, Regex memberRx)
{
    Console.WriteLine($"\n\n{'#',1}## {sectionName} — 키워드 매칭 멤버 ###");
    foreach (var t in types.OrderBy(t => t.Name))
    {
        try
        {
            foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => memberRx.IsMatch(m.Name) && m.DeclaringType?.FullName == t.FullName))
            {
                try
                {
                    string sig = m switch
                    {
                        MethodInfo   mi => $"meth  {mi.ReturnType.Name} {mi.Name}({string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})",
                        PropertyInfo pi => $"prop  {pi.PropertyType.Name} {pi.Name}",
                        FieldInfo    fi => $"field {fi.FieldType.Name} {fi.Name}",
                        _              => $"???   {m.Name}"
                    };
                    Console.WriteLine($"  [{t.Name}]  {sig}");
                }
                catch { }
            }
        }
        catch { }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// 1. 플레이어 스탯 — 전체 덤프
// ════════════════════════════════════════════════════════════════════════════
var psType = types.First(x => x.Name == "PlayerStats");
DumpType(psType, "PlayerStats — 전체");

// ════════════════════════════════════════════════════════════════════════════
// 2. 이동 컨트롤러
// ════════════════════════════════════════════════════════════════════════════
foreach (var name in new[] { "Q3PlayerController", "PlayerMovement", "CharacterController" })
{
    var t = types.FirstOrDefault(x => x.Name == name);
    if (t != null) DumpType(t, name);
}

// ════════════════════════════════════════════════════════════════════════════
// 3. 무기 — WeaponData, Weapon, ShotAction, WeaponItem
// ════════════════════════════════════════════════════════════════════════════
foreach (var name in new[] { "WeaponData", "Weapon", "ShotAction", "WeaponItem",
                              "ShotAction_Raycast", "ReloadHandler", "AmmoHandler" })
{
    var t = types.FirstOrDefault(x => x.Name == name);
    if (t != null) DumpType(t, name);
    else Console.WriteLine($"\n[NOT FOUND] {name}");
}

// ════════════════════════════════════════════════════════════════════════════
// 4. 에너미 — 이름에 Enemy 포함 타입 전부
// ════════════════════════════════════════════════════════════════════════════
SearchTypes(types, "Enemy 타입 목록", new Regex(@"enemy|Enemy|mob|Mob", RegexOptions.IgnoreCase));

foreach (var t in types.Where(x => x.Name.Contains("Enemy", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("EnemyStat", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name))
    DumpType(t, t.Name);

// ════════════════════════════════════════════════════════════════════════════
// 5. 에너미 이동
// ════════════════════════════════════════════════════════════════════════════
foreach (var name in new[] { "EnemyMovement", "EnemyController", "EnemyAI",
                              "ZombieController", "AIController", "AIMovement" })
{
    var t = types.FirstOrDefault(x => x.Name == name);
    if (t != null) DumpType(t, name);
}

// ════════════════════════════════════════════════════════════════════════════
// 6. 스폰 시스템
// ════════════════════════════════════════════════════════════════════════════
SearchTypes(types, "Spawn 타입 목록", new Regex(@"spawn|Spawn|wave|Wave|spawner|Spawner", RegexOptions.IgnoreCase));

foreach (var t in types.Where(x => x.Name.Contains("Spawn", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Wave",  StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name))
    DumpType(t, t.Name);

// ════════════════════════════════════════════════════════════════════════════
// 7. 키워드 멤버 검색 — 주요 수치들
// ════════════════════════════════════════════════════════════════════════════
SearchMembers(types, "체력 관련",      new Regex(@"health|hp|maxHp|MaxHp|curHp|damage|Damage", RegexOptions.IgnoreCase));
SearchMembers(types, "속도 관련",      new Regex(@"speed|Speed|moveSpeed|MoveSpeed|velocity|Velocity", RegexOptions.IgnoreCase));
SearchMembers(types, "장전/탄약 관련", new Regex(@"reload|Reload|magazine|Magazine|ammo|Ammo|clip|Clip", RegexOptions.IgnoreCase));
SearchMembers(types, "쿨다운/발사속도", new Regex(@"cooldown|Cooldown|fireRate|FireRate|attackSpeed|shootDelay|rateOfFire", RegexOptions.IgnoreCase));
SearchMembers(types, "스폰 수/딜레이", new Regex(@"spawnCount|spawnRate|SpawnDelay|spawnInterval|maxEnemies|enemyCount|waveSize", RegexOptions.IgnoreCase));
SearchMembers(types, "에너미 공격력",  new Regex(@"attackDamage|enemyDamage|AttackPower|meleeDamage|MeleeDamage|contactDamage", RegexOptions.IgnoreCase));

// ════════════════════════════════════════════════════════════════════════════
// 8. Health / Damage 컴포넌트
// ════════════════════════════════════════════════════════════════════════════
foreach (var name in new[] { "Health", "HealthComponent", "DamageDealer", "ContactDamage", "Hurtbox" })
{
    var t = types.FirstOrDefault(x => x.Name == name);
    if (t != null) DumpType(t, name);
}

// ════════════════════════════════════════════════════════════════════════════
// 9. GameSettings / DifficultySettings
// ════════════════════════════════════════════════════════════════════════════
foreach (var t in types.Where(x => x.Name.Contains("Setting", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Difficulty", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Balance", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Config", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name))
    DumpType(t, t.Name);

// ════════════════════════════════════════════════════════════════════════════
// 10. MissionAsset — 씬 이름 필드 조사
// ════════════════════════════════════════════════════════════════════════════
var maType = types.FirstOrDefault(x => x.Name == "MissionAsset");
if (maType != null)
    DumpType(maType, "MissionAsset — 전체");
else
    Console.WriteLine("\n[NOT FOUND] MissionAsset");

// 연관 타입도 검색
SearchTypes(types, "Mission 타입 목록", new Regex(@"mission|Mission", RegexOptions.IgnoreCase));
SearchMembers(types, "씬 이름 관련 멤버",
    new Regex(@"scene|Scene|level|Level|sceneName|sceneToLoad|missionScene|levelScene|buildIndex",
              RegexOptions.IgnoreCase));

// ════════════════════════════════════════════════════════════════════════════
// 11. MissionAsset 컬렉션 보유 타입 탐색 — Graveyard 포함 배열/dict 소유자 찾기
// ════════════════════════════════════════════════════════════════════════════
Console.WriteLine("\n\n### MissionAsset 배열/리스트를 가진 멤버 전수 탐색 ###");
foreach (var t in types.OrderBy(x => x.Name))
{
    try
    {
        var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.DeclaringType?.FullName == t.FullName);
        foreach (var m in members)
        {
            try
            {
                Type? memberType = m switch
                {
                    FieldInfo    fi => fi.FieldType,
                    PropertyInfo pi => pi.PropertyType,
                    _              => null
                };
                if (memberType == null) continue;
                string typeName = memberType.Name + (memberType.IsGenericType
                    ? "<" + string.Join(",", memberType.GetGenericArguments().Select(a => a.Name)) + ">"
                    : "");
                // MissionAsset 배열, List, Il2CppArray 포함 여부 확인
                bool hasMission = typeName.Contains("MissionAsset")
                    || (memberType.IsArray && memberType.GetElementType()?.Name == "MissionAsset")
                    || (memberType.IsGenericType && memberType.GetGenericArguments().Any(a => a.Name == "MissionAsset"));
                if (hasMission)
                    Console.WriteLine($"  [{t.Name}]  {(m is FieldInfo ? "field" : "prop")}  {typeName}  {m.Name}");
            }
            catch { }
        }
    }
    catch { }
}

// ════════════════════════════════════════════════════════════════════════════
// 12. EpisodeAsset 전체 덤프
// ════════════════════════════════════════════════════════════════════════════
var epType = types.FirstOrDefault(x => x.Name == "EpisodeAsset");
if (epType != null)
    DumpType(epType, "EpisodeAsset — 전체");
else
    Console.WriteLine("\n[NOT FOUND] EpisodeAsset");

// ════════════════════════════════════════════════════════════════════════════
// 13. 미션 매니저 / 맵 / 셀렉션 관련 타입 덤프
// ════════════════════════════════════════════════════════════════════════════
foreach (var name in new[] {
    "MetaGameMissionSelectionManager", "MissionMapManager",
    "MissionSelectionMapManager", "MissionMapInit",
    "GameDataManager",
    "GameSettings", "PersistentData", "Balancing" })
{
    var t = types.FirstOrDefault(x => x.Name == name);
    if (t != null) DumpType(t, name);
    else Console.WriteLine($"\n[NOT FOUND] {name}");
}

// ════════════════════════════════════════════════════════════════════════════
// 14. "missions" / "playlist" / "episode" 이름 멤버 전수 검색
// ════════════════════════════════════════════════════════════════════════════
SearchMembers(types, "missions/episode/playlist 멤버",
    new Regex(@"missions|Missions|episode|Episode|playlist|Playlist|missionList|MissionList|allMissions|missionData",
              RegexOptions.IgnoreCase));

// ════════════════════════════════════════════════════════════════════════════
// 15. GameDataManager 타입 상세 — availableMissions 반환 타입 풀네임 확인
// ════════════════════════════════════════════════════════════════════════════
// W1. 무기 타이밍 심층 조사 — 발사 간격에 관여하는 float 멤버 전부
// ════════════════════════════════════════════════════════════════════════════
Console.WriteLine("\n\n### 무기 타이밍 심층 조사 ###");
foreach (var name in new[] { "Weapon", "ShotAction", "ShotAction_Raycast",
                              "WeaponData", "WeaponItem", "BurstHandler", "ShotHandler" })
{
    var t = types.FirstOrDefault(x => x.Name == name);
    if (t == null) { Console.WriteLine($"\n[NOT FOUND] {name}"); continue; }
    Console.WriteLine($"\n-- {name} : float 멤버 --");
    var flags2 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    foreach (var p in t.GetProperties(flags2)
        .Where(p => p.DeclaringType?.FullName == t.FullName && p.PropertyType.Name == "Single")
        .OrderBy(p => p.Name))
        Console.WriteLine($"  prop  {p.Name}");
    foreach (var f in t.GetFields(flags2)
        .Where(f => f.DeclaringType?.FullName == t.FullName && f.FieldType.Name == "Single" && !f.Name.StartsWith("<"))
        .OrderBy(f => f.Name))
        Console.WriteLine($"  field {f.Name}");
}

// W2. Shot/Burst/Fire 포함 타입 목록
Console.WriteLine("\n\n### Shot/Burst/Fire 포함 타입 목록 ###");
foreach (var t in types.Where(x => x.Name.Contains("Shot",  StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Burst", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Fire",  StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name))
    Console.WriteLine($"  {t.FullName}");

// W3. delay/timer/interval/wait 멤버 전수 검색
Console.WriteLine("\n\n### delay/timer/interval/wait/pause 관련 멤버 ###");
var timingRx = new Regex(@"delay|Delay|timer|Timer|interval|Interval|wait|Wait|pause|Pause", RegexOptions.IgnoreCase);
foreach (var t in types.OrderBy(t => t.Name))
{
    try
    {
        foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => timingRx.IsMatch(m.Name) && m.DeclaringType?.FullName == t.FullName))
        {
            try
            {
                string sig = m switch
                {
                    MethodInfo   mi => $"meth  {mi.ReturnType.Name} {mi.Name}({string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})",
                    PropertyInfo pi => $"prop  {pi.PropertyType.Name} {pi.Name}",
                    FieldInfo    fi => $"field {fi.FieldType.Name} {fi.Name}",
                    _              => $"???   {m.Name}"
                };
                Console.WriteLine($"  [{t.Name}]  {sig}");
            }
            catch { }
        }
    }
    catch { }
}

// W4. IEnumerator 반환 메서드 (코루틴) 전수
Console.WriteLine("\n\n### IEnumerator 반환 메서드 (코루틴) ###");
foreach (var t in types.OrderBy(x => x.Name))
{
    try
    {
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.DeclaringType?.FullName == t.FullName
                     && (m.ReturnType.Name == "IEnumerator" || m.ReturnType.Name.Contains("Coroutine"))))
        {
            try
            {
                var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  [{t.Name}]  {m.ReturnType.Name} {m.Name}({parms})");
            }
            catch { }
        }
    }
    catch { }
}

// W4b. WeaponHandler + CoTriggerBurst 내부 클래스 전체 덤프
Console.WriteLine("\n\n### WeaponHandler 전체 덤프 ###");
var whType = types.FirstOrDefault(x => x.FullName == "com8com1.SCFPS.WeaponHandler");
if (whType != null) DumpType(whType, "WeaponHandler");
else Console.WriteLine("[NOT FOUND] WeaponHandler");

Console.WriteLine("\n\n### _CoTriggerBurst_d__62 전체 덤프 ###");
var burstCoType = types.FirstOrDefault(x => x.FullName?.Contains("_CoTriggerBurst") == true);
if (burstCoType != null) DumpType(burstCoType, burstCoType.FullName!);
else Console.WriteLine("[NOT FOUND] _CoTriggerBurst*");

// W4c. Weapon 코루틴 관련 필드/메서드
Console.WriteLine("\n\n### Weapon 코루틴/Shoot 관련 멤버 ###");
var wt2 = types.FirstOrDefault(x => x.FullName == "com8com1.SCFPS.Weapon");
if (wt2 != null)
{
    var flags3 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    foreach (var m in wt2.GetMembers(flags3)
        .Where(m => m.DeclaringType?.FullName == wt2.FullName
                 && !m.Name.StartsWith("Native")
                 && (m.Name.Contains("Shoot") || m.Name.Contains("shoot")
                  || m.Name.Contains("Coroutine") || m.Name.Contains("coroutine")
                  || m.Name.Contains("Burst") || m.Name.Contains("burst")
                  || m.Name.Contains("handler") || m.Name.Contains("Handler"))))
    {
        try
        {
            string sig = m switch
            {
                MethodInfo   mi => $"meth  {mi.ReturnType.Name} {mi.Name}({string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})",
                PropertyInfo pi => $"prop  {pi.PropertyType.Name} {pi.Name}",
                FieldInfo    fi => $"field {fi.FieldType.Name} {fi.Name}",
                _              => $"???   {m.Name}"
            };
            Console.WriteLine($"  {sig}");
        }
        catch { }
    }
}

// W5. ShotAction / Weapon 상속 계층 + 파생 클래스
foreach (var rootName in new[] { "ShotAction", "Weapon" })
{
    Console.WriteLine($"\n\n### {rootName} 상속 계층 + 파생 ###");
    var rt = types.FirstOrDefault(x => x.Name == rootName);
    if (rt == null) { Console.WriteLine("  [NOT FOUND]"); continue; }
    var bt2 = rt;
    while (bt2 != null) { Console.WriteLine($"  {bt2.FullName}"); bt2 = bt2.BaseType; }
    Console.WriteLine("  파생 클래스:");
    foreach (var t in types.Where(x => x.BaseType?.Name == rootName).OrderBy(x => x.Name))
        DumpType(t, t.Name);
}

// Quick namespace check
foreach (var tname2 in new[] { "LevelUpScreenManager", "LevelUpOption", "ItemData" })
{
    var t2 = types.FirstOrDefault(x => x.Name == tname2);
    Console.WriteLine($"{tname2} → {t2?.FullName ?? "NOT FOUND"}");
}

// ════════════════════════════════════════════════════════════════════════════
// UPGRADE UI INNER CLASS 덤프
// ════════════════════════════════════════════════════════════════════════════
foreach (var tname in new[] { "UpgradeUI", "LevelUpOption" })
{
    var t2 = types.FirstOrDefault(x => x.Name == tname);
    if (t2 == null) { Console.WriteLine($"\n[NOT FOUND] {tname}"); continue; }
    DumpType(t2, $"{tname} — 전체");
}

// ════════════════════════════════════════════════════════════════════════════
// ITEM / CARD SELECTION 발견 섹션
// ════════════════════════════════════════════════════════════════════════════

// 1. 타입 이름 검색 — 카드/보상/업그레이드 UI 클래스
SearchTypes(types, "Card/Item/Reward/Upgrade 타입",
    new Regex(@"card|reward|upgrade|pickup|item|select|choice|picker",
              RegexOptions.IgnoreCase));

// 2. 메서드/프로퍼티/필드 검색
SearchMembers(types, "선택 관련 메서드/필드",
    new Regex(@"SelectCard|PickItem|ChooseReward|ConfirmSelection|OnCardSelect"
            + @"|OnItemSelect|OnUpgradeSelect|AddItem|PickUpgrade|SelectUpgrade"
            + @"|cardIndex|itemIndex|selectedCard|selectedItem|currentCards"
            + @"|availableCards|rewardCards|upgradeCards",
              RegexOptions.IgnoreCase));

// 3. PlayerInventory 전체 덤프
var invType = types.FirstOrDefault(x => x.Name == "PlayerInventory");
if (invType != null) DumpType(invType, "PlayerInventory — 전체");
else Console.WriteLine("\n[NOT FOUND] PlayerInventory");

// 4. LevelUpAway / HasLevelUpAway 관련 — 레벨업 선택 기능 확인
SearchMembers(types, "Away/LevelUpAway 관련",
    new Regex(@"away|Away|levelUpAway|LevelUpAway|HasAway|PickAway",
              RegexOptions.IgnoreCase));

// 5. Away/PickUp 포함 타입 전체 덤프
foreach (var t in types.Where(x => x.Name.Contains("Away", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("PickUp", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Reward", StringComparison.OrdinalIgnoreCase)
                                 || x.Name.Contains("Card", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name))
    DumpType(t, t.Name);

// ════════════════════════════════════════════════════════════════════════════
Console.WriteLine("\n\n### GameDataManager 타입 상세 ###");
var gdmType2 = types.FirstOrDefault(x => x.Name == "GameDataManager");
if (gdmType2 != null)
{
    // 상속 계층
    Console.WriteLine("상속 계층:");
    var bt = gdmType2;
    while (bt != null) { Console.WriteLine($"  {bt.FullName}  [{bt.Assembly?.GetName().Name}]"); bt = bt.BaseType; }

    // availableMissions / availablePlayableCharacters 타입 풀네임
    foreach (var pname in new[] { "availableMissions", "availablePlayableCharacters",
                                   "availableEpisodes" })
    {
        var p = gdmType2.GetProperty(pname,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null)
        {
            Console.Write($"\n{pname}: {p.PropertyType.FullName}");
            if (p.PropertyType.IsGenericType)
            {
                Console.Write("  Generic args: ");
                Console.Write(string.Join(", ", p.PropertyType.GetGenericArguments().Select(a => a.FullName)));
            }
            Console.WriteLine();
        }
    }
}
