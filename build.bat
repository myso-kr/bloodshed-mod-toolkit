@echo off
echo Building BloodshedModToolkit...
dotnet build -c Release
if %ERRORLEVEL% NEQ 0 (
    echo Build FAILED. BepInEx interop 어셈블리가 생성되었는지 확인하세요.
    pause
    exit /b 1
)

set TARGET=D:\SteamLibrary\steamapps\common\Bloodshed\BepInEx\plugins\BloodshedModToolkit.dll
set SOURCE=bin\Release\net6.0\BloodshedModToolkit.dll

echo Copying to BepInEx plugins...
copy /Y "%SOURCE%" "%TARGET%"
if %ERRORLEVEL% EQU 0 (
    echo SUCCESS: %TARGET%
) else (
    echo WARN: BepInEx\plugins 폴더가 없습니다. BepInEx를 먼저 설치하고 게임을 한 번 실행하세요.
)
pause
