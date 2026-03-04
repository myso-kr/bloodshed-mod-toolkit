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
