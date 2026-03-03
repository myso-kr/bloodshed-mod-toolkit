Add-Type -AssemblyName System.Reflection.Metadata

$interopDir  = "D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx\interop"
$targetAssem = "$interopDir\Assembly-CSharp.dll"

# MetadataLoadContext: 의존 DLL 경로를 모두 알려줘야 함
$resolver = [System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
$paths = [System.IO.Directory]::GetFiles($interopDir, "*.dll") +
         [System.IO.Directory]::GetFiles($resolver,   "*.dll")

$mlc   = New-Object System.Reflection.MetadataLoadContext([System.Reflection.PathAssemblyResolver]::new($paths))
$asm   = $mlc.LoadFromAssemblyPath($targetAssem)
$types = $asm.GetTypes()
Write-Host "Total types: $($types.Count)"

function Search($label, $pattern) {
    Write-Host ""
    Write-Host "=== $label ==="
    $types | Where-Object { $_.Name -match $pattern } | ForEach-Object {
        Write-Host "  $($_.FullName)"
    }
}

Search "Health / Damage"    'Health|Damage|Hp|Hurt'
Search "Currency / Gem"     'Gem|Currency|Money|Coin|Gold'
Search "Stats"              'Stat|Might|Armor|Agility|Attribute'
Search "Player (prefix)"    '^Player'
Search "Cooldown / Ability" 'Cooldown|Ability|Spell|Skill'
Search "Experience / Level" 'Exp|Level|XP|Experience|Reviv'
Search "Move / Speed"       'Move|Speed|Locomotion|Character'

$mlc.Dispose()
