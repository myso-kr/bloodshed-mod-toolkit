<#
.SYNOPSIS
    Bloodshed Mod Toolkit 릴리즈 빌드 및 GitHub Release 업로드 스크립트.

.DESCRIPTION
    1. 로컬에서 Release 빌드 (실제 interop DLL 사용, 게임 실행 필요)
    2. BepInEx/plugins/ 구조로 zip 패키지 생성
    3. git 태그 생성 및 push
    4. gh release create 또는 upload 로 DLL + zip 업로드

.PARAMETER Version
    릴리즈 버전 문자열 (예: "1.0.0"). 지정하지 않으면 csproj 에서 자동 추출.

.PARAMETER BepInExPath
    BepInEx 설치 경로. 기본값은 게임 설치 경로의 BepInEx 폴더.

.PARAMETER SkipBuild
    빌드를 건너뛰고 bin/Release/ 의 기존 DLL 을 사용합니다.
    pre-push 훅에서 이미 빌드한 경우 이중 빌드 방지용.

.PARAMETER DraftOnly
    GitHub Release 를 Draft 상태로만 생성하고 Publish 하지 않음.

.PARAMETER NoBrowser
    릴리즈 완료 후 브라우저를 자동으로 열지 않음 (훅/자동화 호출용).

.EXAMPLE
    .\scripts\Publish-Release.ps1                        # 수동 릴리즈 (빌드 포함)
    .\scripts\Publish-Release.ps1 -SkipBuild -NoBrowser  # 훅에서 호출 (빌드 생략)
    .\scripts\Publish-Release.ps1 -DraftOnly             # Draft 로만 생성
#>
param(
    [string]$Version     = "",
    [string]$BepInExPath = "D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx",
    [switch]$SkipBuild,
    [switch]$DraftOnly,
    [switch]$NoBrowser
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
Push-Location $root

try {

# ── 버전 결정 ─────────────────────────────────────────────────────────────────
if (-not $Version) {
    $xml     = [xml](Get-Content "BloodshedModToolkit.csproj")
    $Version = ($xml.Project.PropertyGroup | Where-Object { $_.Version } |
                Select-Object -First 1).Version
}
if (-not $Version) { throw "버전을 결정할 수 없습니다. -Version 파라미터를 직접 지정하세요." }
$tag = "v$Version"
Write-Host "Release: $Version  (tag: $tag)" -ForegroundColor Cyan

# ── gh CLI 확인 ───────────────────────────────────────────────────────────────
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI 가 설치되어 있지 않습니다. https://cli.github.com 에서 설치하세요."
}

$dll = "bin\Release\net6.0\BloodshedModToolkit.dll"

# ── 1. 빌드 ───────────────────────────────────────────────────────────────────
if ($SkipBuild) {
    Write-Host "`n[1/4] 빌드 생략 (-SkipBuild)" -ForegroundColor DarkGray
    if (-not (Test-Path $dll)) { throw "DLL 없음: $dll  (-SkipBuild 사용 전 빌드 필요)" }
} else {
    if (-not (Test-Path "$BepInExPath\core\BepInEx.Core.dll")) {
        throw "BepInEx 코어를 찾을 수 없습니다: $BepInExPath"
    }
    Write-Host "`n[1/4] 빌드 중..." -ForegroundColor Yellow
    dotnet build -c Release "-p:BepInExPath=$BepInExPath" --no-incremental
    if ($LASTEXITCODE -ne 0) { throw "빌드 실패" }
    Write-Host "  완료: $dll" -ForegroundColor Green
}

# ── 2. zip 패키지 ─────────────────────────────────────────────────────────────
Write-Host "`n[2/4] 패키지 생성 중..." -ForegroundColor Yellow
$zipName    = "BloodshedModToolkit_$tag.zip"
$stagingDir = "release-staging"

Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
$pluginsDir = "$stagingDir\BepInEx\plugins"
New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null
Copy-Item $dll        $pluginsDir
Copy-Item "README.md" $stagingDir

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipName -Force
Remove-Item $stagingDir -Recurse -Force
Write-Host "  완료: $zipName" -ForegroundColor Green

# ── 3. git 태그 ───────────────────────────────────────────────────────────────
Write-Host "`n[3/4] git 태그..." -ForegroundColor Yellow
$existingTag = git tag -l $tag
if ($existingTag) {
    Write-Host "  태그 $tag 이미 존재, 건너뜀" -ForegroundColor DarkYellow
} else {
    git tag $tag
    git push origin $tag
    Write-Host "  $tag push 완료" -ForegroundColor Green
}

# ── 4. GitHub Release ─────────────────────────────────────────────────────────
Write-Host "`n[4/4] GitHub Release 업로드..." -ForegroundColor Yellow

$null = gh release view $tag 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  기존 릴리즈에 파일 추가..." -ForegroundColor DarkYellow
    gh release upload $tag $zipName "$dll#BloodshedModToolkit.dll" --clobber
    if (-not $DraftOnly) { gh release edit $tag --draft=false }
} else {
    # 빈 문자열이 인자로 전달되지 않도록 배열 splatting 사용
    $releaseArgs = @(
        $tag, $zipName, "$dll#BloodshedModToolkit.dll",
        '--title', "Bloodshed Mod Toolkit v$Version",
        '--generate-notes'
    )
    if ($DraftOnly) { $releaseArgs += '--draft' }
    gh release create @releaseArgs
}

Write-Host "`nRelease $tag 완료!" -ForegroundColor Cyan
if (-not $NoBrowser) { gh release view $tag --web }

} finally {
    Pop-Location
    Remove-Item "release-staging" -Recurse -Force -ErrorAction SilentlyContinue
}
