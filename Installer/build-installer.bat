@echo off
setlocal

echo ============================================================
echo  Bloodshed Mod Toolkit Installer Builder
echo ============================================================
echo.

echo [1/2] Building BloodshedModToolkit.dll (Release)...
cd /d "%~dp0.."
dotnet build -c Release
if ERRORLEVEL 1 (
    echo ERROR: Mod DLL build failed.
    pause
    exit /b 1
)

echo.
echo [2/2] Publishing installer as single EXE...
cd /d "%~dp0"
dotnet publish -c Release -r win-x64 --self-contained ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o publish

if ERRORLEVEL 1 (
    echo ERROR: Installer publish failed.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo  Done!  Output: %~dp0publish\BloodshedModToolkitInstaller.exe
echo ============================================================
echo.
pause
