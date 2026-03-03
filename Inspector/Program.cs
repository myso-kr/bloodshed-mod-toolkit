using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

var interopDir = @"D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx\interop";
var coreDir    = @"D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx\core";
var targetPath = Path.Combine(interopDir, "Assembly-CSharp.dll");

var allDlls = Directory.GetFiles(interopDir, "*.dll")
    .Concat(Directory.GetFiles(coreDir, "*.dll"))
    .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
    .ToArray();

using var mlc = new MetadataLoadContext(new PathAssemblyResolver(allDlls));
var asm   = mlc.LoadFromAssemblyPath(targetPath);
var types = asm.GetTypes();

// ── ShotAction 완전 분석 (special name 메서드 포함) ──────────────────────────
Console.WriteLine("\n=== ShotAction — 모든 메서드 (special 포함) ===");
var sa = types.FirstOrDefault(x => x.Name == "ShotAction");
if (sa != null)
{
    // 프로퍼티
    foreach (var p in sa.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(p => !p.Name.StartsWith("Native") && p.Name is not ("Pointer" or "ObjectClass"))
        .OrderBy(p => p.Name))
        Console.WriteLine($"  prop  {p.PropertyType.Name} {p.Name}");

    // 메서드 (special name 포함)
    foreach (var m in sa.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(m => m.DeclaringType?.FullName == sa.FullName)
        .OrderBy(m => m.Name))
    {
        try
        {
            var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            var flag  = m.IsSpecialName ? "[special] " : "";
            Console.WriteLine($"  meth  {flag}{m.ReturnType.Name} {m.Name}({parms})");
        }
        catch { Console.WriteLine($"  meth  ??? {m.Name}(...)"); }
    }

    // 쿨다운 관련 필드
    Console.WriteLine("\n  -- Cooldown 관련 필드 --");
    foreach (var f in sa.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
    {
        if (f.Name.IndexOf("cooldown", StringComparison.OrdinalIgnoreCase) >= 0 ||
            f.Name.IndexOf("Cooldown", StringComparison.OrdinalIgnoreCase) >= 0 ||
            f.Name.IndexOf("timer",    StringComparison.OrdinalIgnoreCase) >= 0 ||
            f.Name.IndexOf("fireRate", StringComparison.OrdinalIgnoreCase) >= 0)
            Console.WriteLine($"  field {f.FieldType.Name} {f.Name}");
    }
}
else Console.WriteLine("[NOT FOUND] ShotAction");

// ── ShotAction_Raycast / ShotAction_Default 상세 ─────────────────────────────
foreach (var tName in new[] { "ShotAction_Raycast", "ShotAction_Default" })
{
    var t = types.FirstOrDefault(x => x.Name == tName);
    Console.WriteLine($"\n=== {tName} ===");
    if (t == null) { Console.WriteLine("  [NOT FOUND]"); continue; }
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(m => m.DeclaringType?.FullName == t.FullName)
        .OrderBy(m => m.Name))
    {
        try
        {
            var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            Console.WriteLine($"  meth  {m.ReturnType.Name} {m.Name}({parms})");
        }
        catch { Console.WriteLine($"  meth  ??? {m.Name}(...)"); }
    }
}

// ── PlayerStats — 경험치 / 레벨 관련 멤버 ──────────────────────────────────
Console.WriteLine("\n=== PlayerStats — experience / level 관련 멤버 ===");
var ps = types.FirstOrDefault(x => x.Name == "PlayerStats");
if (ps != null)
{
    var rXp = new Regex(@"exp|xp|level|levelup|nextlevel", RegexOptions.IgnoreCase);
    foreach (var m in ps.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(m => rXp.IsMatch(m.Name) && m.DeclaringType?.FullName == ps.FullName
                 && m.MemberType is MemberTypes.Method or MemberTypes.Property or MemberTypes.Field)
        .OrderBy(m => m.Name))
    {
        try
        {
            if (m is MethodInfo mi)
            {
                var parms = string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  meth  {mi.ReturnType.Name} {mi.Name}({parms})");
            }
            else if (m is PropertyInfo pi)
                Console.WriteLine($"  prop  {pi.PropertyType.Name} {pi.Name}");
            else if (m is FieldInfo fi)
                Console.WriteLine($"  field {fi.FieldType.Name} {fi.Name}");
        }
        catch { }
    }
}
else Console.WriteLine("[NOT FOUND] PlayerStats");

// ── Cooldown / fireRate 관련 타입 전체 탐색 ─────────────────────────────────
Console.WriteLine("\n=== StartCooldown / SetCooldown / fireRate 멤버 (전체) ===");
var rCd = new Regex(@"StartCooldown|SetCooldown|EndCooldown|fireRate|FireRate|fireCooldown|shootCooldown", RegexOptions.IgnoreCase);
foreach (var t in types)
{
    try
    {
        foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => rCd.IsMatch(m.Name) && m.DeclaringType?.FullName == t.FullName))
        {
            try
            {
                if (m is MethodInfo mi)
                {
                    var parms = string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Console.WriteLine($"  [{t.FullName}]  {mi.ReturnType.Name} {mi.Name}({parms})");
                }
                else if (m is PropertyInfo pi)
                    Console.WriteLine($"  [{t.FullName}]  (prop) {pi.PropertyType.Name} {pi.Name}");
                else if (m is FieldInfo fi)
                    Console.WriteLine($"  [{t.FullName}]  (field) {fi.FieldType.Name} {fi.Name}");
            }
            catch { }
        }
    }
    catch { }
}
