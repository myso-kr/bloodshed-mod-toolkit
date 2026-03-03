<#
.SYNOPSIS
    git hooks 를 .git/hooks/ 에 설치합니다.

.DESCRIPTION
    scripts/hooks/ 의 훅 파일을 .git/hooks/ 에 복사하고 실행 권한을 부여합니다.
    이미 존재하는 훅은 .bak 으로 백업됩니다.

.EXAMPLE
    .\scripts\Install-Hooks.ps1
#>
param(
    [switch]$Uninstall
)

$root      = Split-Path $PSScriptRoot -Parent
$hooksDir  = Join-Path $root ".git\hooks"
$srcDir    = Join-Path $PSScriptRoot "hooks"

if (-not (Test-Path $hooksDir)) {
    throw ".git/hooks 디렉토리가 없습니다. git 저장소 루트에서 실행하세요."
}

$hooks = @("pre-commit", "pre-push")

foreach ($hook in $hooks) {
    $dest = Join-Path $hooksDir $hook
    $src  = Join-Path $srcDir   $hook

    if ($Uninstall) {
        if (Test-Path $dest) {
            Remove-Item $dest -Force
            Write-Host "제거됨: $dest" -ForegroundColor Yellow
        }
        # 백업 복구
        $bak = "$dest.bak"
        if (Test-Path $bak) {
            Rename-Item $bak $dest
            Write-Host "백업 복구: $bak -> $dest" -ForegroundColor DarkYellow
        }
        continue
    }

    if (-not (Test-Path $src)) {
        Write-Warning "훅 파일 없음, 건너뜀: $src"
        continue
    }

    # 기존 훅 백업
    if (Test-Path $dest) {
        $bak = "$dest.bak"
        Copy-Item $dest $bak -Force
        Write-Host "백업: $dest -> $bak" -ForegroundColor DarkYellow
    }

    Copy-Item $src $dest -Force

    # Git Bash 호환: LF 변환 + BOM 없는 UTF-8 으로 저장
    $content = Get-Content $dest -Raw
    $content = $content -replace "`r`n", "`n"
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($dest, $content, $utf8NoBom)

    Write-Host "설치됨: $dest" -ForegroundColor Green
}

if (-not $Uninstall) {
    Write-Host "`nhooks 설치 완료." -ForegroundColor Cyan
    Write-Host "  pre-commit : 커밋 전 stub 빌드 검증" -ForegroundColor White
    Write-Host "  pre-push   : push 전 실제 빌드 + plugins/ 자동 배포" -ForegroundColor White
}
