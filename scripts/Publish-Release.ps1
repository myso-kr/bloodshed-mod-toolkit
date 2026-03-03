<#
.SYNOPSIS
    Bloodshed Mod Toolkit 릴리즈 빌드 및 GitHub Release 업로드 스크립트.

.DESCRIPTION
    1. 로컬에서 Release 빌드 (실제 interop DLL 사용, 게임 실행 필요)
    2. BepInEx/plugins/ 구조로 zip 패키지 생성
    3. git 태그 생성 및 push
    4. gh release create 또는 upload 로 DLL + zip 업로드

.PARAMETER Version
    릴리즈 버전 문자열 (예: "1.0.0"). 지정하지 않으면 csproj 에서 자동 추출 시도.

.PARAMETER BepInExPath
    BepInEx 설치 경로. 기본값은 게임 설치 경로의 BepInEx 폴더.

.PARAMETER DraftOnly
    GitHub Release 를 Draft 상태로만 생성하고 Publish 하지 않음.

.EXAMPLE
    .\scripts\Publish-Release.ps1 -Version 1.0.0
    .\scripts\Publish-Release.ps1 -Version 1.1.0 -DraftOnly
#>
param(
    [string]$Version    = "",
    [string]$BepInExPath = "D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx",
    [switch]$DraftOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── 루트 디렉토리 확인 ────────────────────────────────────────────────────────
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root

try {

# ── 버전 결정 ─────────────────────────────────────────────────────────────────
if (-not $Version) {
    $xml = [xml](Get-Content "BloodshedModToolkit.csproj")
    $Version = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $Version) {
        # Plugin.cs 에서 PLUGIN_VERSION 추출
        $line = Select-String -Path "Plugin.cs" -Pattern 'PLUGIN_VERSION\s*=\s*"([^"]+)"'
        $Version = $line.Matches[0].Groups[1].Value
    }
}
if (-not $Version) { throw "버전을 결정할 수 없습니다. -Version 파라미터를 직접 지정하세요." }
$tag = "v$Version"
Write-Host "Release version: $Version (tag: $tag)" -ForegroundColor Cyan

# ── gh CLI 확인 ───────────────────────────────────────────────────────────────
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI 가 설치되어 있지 않습니다. https://cli.github.com 에서 설치하세요."
}

# ── BepInEx 경로 검증 ─────────────────────────────────────────────────────────
if (-not (Test-Path "$BepInExPath\core\BepInEx.Core.dll")) {
    throw "BepInEx 코어 DLL 을 찾을 수 없습니다: $BepInExPath\core\BepInEx.Core.dll`n게임을 한 번 실행해 BepInEx interop 를 생성하세요."
}

# ── 1. Release 빌드 ───────────────────────────────────────────────────────────
Write-Host "`n[1/4] 빌드 중..." -ForegroundColor Yellow
dotnet build -c Release "-p:BepInExPath=$BepInExPath" --no-incremental
if ($LASTEXITCODE -ne 0) { throw "빌드 실패" }

$dll = "bin\Release\net6.0\BloodshedModToolkit.dll"
if (-not (Test-Path $dll)) { throw "빌드 결과물을 찾을 수 없습니다: $dll" }
Write-Host "  빌드 완료: $dll" -ForegroundColor Green

# ── 2. zip 패키지 생성 ────────────────────────────────────────────────────────
Write-Host "`n[2/4] 패키지 생성 중..." -ForegroundColor Yellow
$zipName    = "BloodshedModToolkit_$tag.zip"
$stagingDir = "release-staging"

Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
$pluginsDir = "$stagingDir\BepInEx\plugins"
New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null
Copy-Item $dll         $pluginsDir
Copy-Item "README.md"  $stagingDir

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipName -Force
Remove-Item $stagingDir -Recurse -Force
Write-Host "  패키지 생성 완료: $zipName" -ForegroundColor Green

# ── 3. git 태그 ───────────────────────────────────────────────────────────────
Write-Host "`n[3/4] git 태그 생성 및 push..." -ForegroundColor Yellow
$existingTag = git tag -l $tag
if ($existingTag) {
    Write-Host "  태그 $tag 이미 존재함, 건너뜀" -ForegroundColor DarkYellow
} else {
    git tag $tag
    git push origin $tag
    Write-Host "  태그 $tag push 완료" -ForegroundColor Green
}

# ── 4. GitHub Release ─────────────────────────────────────────────────────────
Write-Host "`n[4/4] GitHub Release 업로드..." -ForegroundColor Yellow

# 이미 Release 가 있으면 파일만 추가 (release.yml 이 draft 생성했을 수 있음)
$releaseExists = gh release view $tag 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  기존 Release 에 파일 추가 (draft → publish)" -ForegroundColor DarkYellow
    gh release upload $tag $zipName "$dll#BloodshedModToolkit.dll" --clobber
    if (-not $DraftOnly) {
        gh release edit $tag --draft=false
    }
} else {
    $draftFlag = if ($DraftOnly) { "--draft" } else { "" }
    gh release create $tag $zipName "$dll#BloodshedModToolkit.dll" `
        --title "Bloodshed Mod Toolkit v$Version" `
        --generate-notes `
        $draftFlag
}

Write-Host "`nRelease $tag 완료!" -ForegroundColor Cyan
gh release view $tag --web

} finally {
    Pop-Location
    Remove-Item "release-staging" -Recurse -Force -ErrorAction SilentlyContinue
}
