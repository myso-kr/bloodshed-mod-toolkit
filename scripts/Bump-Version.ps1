<#
.SYNOPSIS
    버전을 올리고 csproj 와 Plugin.cs 를 동시에 업데이트합니다.

.DESCRIPTION
    csproj <Version> 이 단일 진실 원천입니다.
    이 스크립트는 지정한 컴포넌트를 올린 뒤 Plugin.cs 의 PLUGIN_VERSION 을 동기화합니다.

    Semantic Versioning (MAJOR.MINOR.PATCH):
      PATCH — 버그 수정, 기존 치트 동작 개선
      MINOR — 새 치트/기능 추가
      MAJOR — 구조 변경, 하위 호환 불가

.PARAMETER Bump
    올릴 컴포넌트: Major | Minor | Patch (기본값: Patch)

.PARAMETER To
    직접 버전을 지정합니다 (예: "2.0.0"). Bump 보다 우선합니다.

.EXAMPLE
    .\scripts\Bump-Version.ps1              # 1.0.0 → 1.0.1
    .\scripts\Bump-Version.ps1 -Bump Minor  # 1.0.1 → 1.1.0
    .\scripts\Bump-Version.ps1 -Bump Major  # 1.1.0 → 2.0.0
    .\scripts\Bump-Version.ps1 -To 1.2.3    # → 1.2.3
#>
param(
    [ValidateSet("Major", "Minor", "Patch")]
    [string]$Bump = "Patch",
    [string]$To   = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root             = Split-Path $PSScriptRoot -Parent
$csproj           = Join-Path $root "BloodshedModToolkit.csproj"
$installerCsproj  = Join-Path $root "Installer\BloodshedModToolkitInstaller.csproj"
$pluginCs         = Join-Path $root "Plugin.cs"

# ── 현재 버전 읽기 ────────────────────────────────────────────────────────────
$xml = [xml](Get-Content $csproj -Encoding UTF8)
$current = ($xml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $current) { throw "csproj 에서 <Version> 을 찾을 수 없습니다." }

$parts = $current -split '\.' | ForEach-Object { [int]$_ }
if ($parts.Count -ne 3) { throw "버전 형식이 올바르지 않습니다: $current (MAJOR.MINOR.PATCH 형식 필요)" }

# ── 새 버전 계산 ──────────────────────────────────────────────────────────────
if ($To) {
    $next = $To
    if ($next -notmatch '^\d+\.\d+\.\d+$') { throw "버전 형식 오류: $next" }
} else {
    $major, $minor, $patch = $parts
    switch ($Bump) {
        "Major" { $major++; $minor = 0; $patch = 0 }
        "Minor" { $minor++;             $patch = 0 }
        "Patch" { $patch++ }
    }
    $next = "$major.$minor.$patch"
}

if ($next -eq $current) {
    Write-Host "버전이 이미 $current 입니다. 변경 없음." -ForegroundColor DarkYellow
    exit 0
}

Write-Host "$current  →  $next" -ForegroundColor Cyan

# ── csproj 업데이트 ───────────────────────────────────────────────────────────
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$versionPattern = "<Version>$([regex]::Escape($current))</Version>"
$versionReplacement = "<Version>$next</Version>"

$csprojContent = Get-Content $csproj -Raw -Encoding UTF8
$csprojContent = $csprojContent -replace $versionPattern, $versionReplacement
[System.IO.File]::WriteAllText($csproj, $csprojContent, $utf8NoBom)
Write-Host "  BloodshedModToolkit.csproj <Version> 업데이트" -ForegroundColor Green

$installerContent = Get-Content $installerCsproj -Raw -Encoding UTF8
$installerContent = $installerContent -replace $versionPattern, $versionReplacement
[System.IO.File]::WriteAllText($installerCsproj, $installerContent, $utf8NoBom)
Write-Host "  Installer/BloodshedModToolkitInstaller.csproj <Version> 업데이트" -ForegroundColor Green

# ── Plugin.cs 업데이트 ────────────────────────────────────────────────────────
$pluginContent = Get-Content $pluginCs -Raw -Encoding UTF8
$pluginContent = $pluginContent -replace `
    "(PLUGIN_VERSION\s*=\s*"")[^""]*("")",
    "`${1}$next`$2"
[System.IO.File]::WriteAllText($pluginCs, $pluginContent, $utf8NoBom)
Write-Host "  Plugin.cs PLUGIN_VERSION 업데이트" -ForegroundColor Green

# ── 검증 ──────────────────────────────────────────────────────────────────────
$verifyXml = [xml](Get-Content $csproj -Encoding UTF8)
$verifyVer = ($verifyXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
$verifyInstallerXml = [xml](Get-Content $installerCsproj -Encoding UTF8)
$verifyInstallerVer = ($verifyInstallerXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
$verifyPlugin = (Select-String -Path $pluginCs -Pattern 'PLUGIN_VERSION\s*=\s*"([^"]+)"').Matches[0].Groups[1].Value

if ($verifyVer -ne $next -or $verifyPlugin -ne $next -or $verifyInstallerVer -ne $next) {
    throw "검증 실패: csproj=$verifyVer installer=$verifyInstallerVer Plugin.cs=$verifyPlugin (기대값: $next)"
}

Write-Host ""
Write-Host "버전 $next 준비 완료." -ForegroundColor Cyan
Write-Host "다음 단계:" -ForegroundColor White
Write-Host "  git add BloodshedModToolkit.csproj Installer/BloodshedModToolkitInstaller.csproj Plugin.cs" -ForegroundColor DarkGray
Write-Host "  git commit -m `"chore: bump version to $next`"" -ForegroundColor DarkGray
Write-Host "  .\scripts\Publish-Release.ps1" -ForegroundColor DarkGray
