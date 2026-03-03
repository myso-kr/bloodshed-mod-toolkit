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
echo [2/2] Building installer with NSIS...
cd /d "%~dp0"

set MAKENSIS=
if exist "C:\Program Files (x86)\NSIS\makensis.exe" set MAKENSIS=C:\Program Files (x86)\NSIS\makensis.exe
if exist "C:\Program Files\NSIS\makensis.exe"       set MAKENSIS=C:\Program Files\NSIS\makensis.exe

if "%MAKENSIS%"=="" (
    echo ERROR: NSIS not found.
    echo Install from: https://nsis.sourceforge.io/
    pause
    exit /b 1
)

if not exist "publish" mkdir publish
"%MAKENSIS%" bloodshed.nsi
if ERRORLEVEL 1 (
    echo ERROR: NSIS build failed.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo  Done!  Output: %~dp0publish\BloodshedModToolkitInstaller.exe
echo ============================================================
echo.
pause
