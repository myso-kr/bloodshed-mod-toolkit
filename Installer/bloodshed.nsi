Unicode True
SetCompressor /SOLID lzma

!define APPNAME    "Bloodshed Mod Toolkit"
!define VERSION    "1.0.157"
!define BEPINEX_URL "https://builds.bepinex.dev/projects/bepinex_be/697/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.697%2B1cd1765.zip"
!define GITHUB_REPO "myso-kr/bloodshed-mod-toolkit"
!define MOD_DLL_URL "https://github.com/${GITHUB_REPO}/releases/latest/download/BloodshedModToolkit.dll"

Name    "${APPNAME} v${VERSION}"
OutFile "publish\BloodshedModToolkitInstaller.exe"
Icon    "bloodshed.ico"
RequestExecutionLevel admin

!include "nsDialogs.nsh"
!include "LogicLib.nsh"

Var hPathText
Var hBrowseBtn
Var hBepInExChk
Var hModDllChk
Var hProgress
Var hStatus
Var GamePath
Var DoInstallBepInEx
Var DoInstallModDll

Page custom MainPage MainPageLeave

Section ""
SectionEnd

; - .onInit -
Function .onInit
    Call DetectGamePath
FunctionEnd

; - DetectGamePath -
; Writes a PowerShell script to %TEMP% and runs it to search all Steam library
; folders (via libraryfolders.vdf) for the Bloodshed install directory.
; Uses NSIS double-quoted FileWrite strings: $$ = literal $, $\" = literal "
Function DetectGamePath
    StrCpy $GamePath ""

    FileOpen $0 "$TEMP\bmti_detect.ps1" w
    FileWrite $0 "$$reg = 'HKCU:\SOFTWARE\Valve\Steam'$\n"
    FileWrite $0 "try { $$sp = (Get-ItemProperty $$reg -EA Stop).SteamPath } catch { $$sp = '' }$\n"
    FileWrite $0 "$$libs = @()$\n"
    FileWrite $0 "if ($$sp) {$\n"
    FileWrite $0 "    $$vdf = Join-Path $$sp 'steamapps\libraryfolders.vdf'$\n"
    FileWrite $0 "    if (Test-Path $$vdf) {$\n"
    FileWrite $0 "        foreach ($$line in (Get-Content $$vdf)) {$\n"
    FileWrite $0 "            $$m = [regex]::Match($$line, '[A-Z]:\\[^$\"]+')$\n"
    FileWrite $0 "            if ($$m.Success) { $$libs += ($$m.Value.Trim('$\"') -replace '\\\\', '\') }$\n"
    FileWrite $0 "        }$\n"
    FileWrite $0 "    }$\n"
    FileWrite $0 "    $$libs += $$sp$\n"
    FileWrite $0 "}$\n"
    FileWrite $0 "$$libs += 'C:\Program Files (x86)\Steam'$\n"
    FileWrite $0 "$$libs += 'C:\Program Files\Steam'$\n"
    FileWrite $0 "foreach ($$lib in $$libs) {$\n"
    FileWrite $0 "    $$p = Join-Path $$lib 'steamapps\common\Bloodshed'$\n"
    FileWrite $0 "    if (Test-Path (Join-Path $$p 'Bloodshed.exe')) { [Console]::Write($$p); exit 0 }$\n"
    FileWrite $0 "}$\n"
    FileClose $0

    nsExec::ExecToStack "powershell -NoProfile -ExecutionPolicy Bypass -File $\"$TEMP\bmti_detect.ps1$\""
    Pop $0   ; exit code
    Pop $1   ; stdout (game path, no trailing newline)
    Delete "$TEMP\bmti_detect.ps1"

    StrCpy $GamePath $1
FunctionEnd

; - MainPage -
Function MainPage
    nsDialogs::Create 1018
    Pop $0

    ; Title
    ${NSD_CreateLabel} 0 0 100% 12u "${APPNAME} v${VERSION} - Installer"
    Pop $0
    CreateFont $1 "$(^Font)" 9 700
    SendMessage $0 ${WM_SETFONT} $1 1

    ; Separator
    ${NSD_CreateHLine} 0 15u 100% 1u ""
    Pop $0

    ; Game Path label
    ${NSD_CreateLabel} 0 22u 25u 10u "Game Path:"
    Pop $0

    ; Path textbox
    ${NSD_CreateDirRequest} 28u 21u 250u 12u "$GamePath"
    Pop $hPathText

    ; Browse button
    ${NSD_CreateBrowseButton} 282u 20u 50u 14u "..."
    Pop $hBrowseBtn
    ${NSD_OnClick} $hBrowseBtn BrowseClicked

    ; Separator
    ${NSD_CreateHLine} 0 37u 100% 1u ""
    Pop $0

    ; BepInEx checkbox
    ${NSD_CreateCheckBox} 0 43u 100% 12u "Install BepInEx 6.x (IL2CPP) - required mod loader"
    Pop $hBepInExChk
    ${NSD_SetState} $hBepInExChk ${BST_CHECKED}

    ; Mod DLL checkbox
    ${NSD_CreateCheckBox} 0 57u 100% 12u "Install BloodshedModToolkit.dll to BepInEx/plugins/"
    Pop $hModDllChk
    ${NSD_SetState} $hModDllChk ${BST_CHECKED}

    ; Separator
    ${NSD_CreateHLine} 0 73u 100% 1u ""
    Pop $0

    ; Progress bar
    ${NSD_CreateProgressBar} 0 79u 100% 12u ""
    Pop $hProgress
    SendMessage $hProgress 0x401 0 0   ; PBM_SETRANGE lo=0 hi=100 (MAKELPARAM)
    SendMessage $hProgress 0x401 0 6553600  ; range 0..100 (high word = 100)

    ; Status label
    ${NSD_CreateLabel} 0 95u 100% 10u "Ready."
    Pop $hStatus

    ; Customize buttons
    GetDlgItem $0 $HWNDPARENT 1   ; IDOK = Next = "Install"
    SendMessage $0 ${WM_SETTEXT} 0 "STR:Install"

    GetDlgItem $0 $HWNDPARENT 3   ; Back button
    ShowWindow $0 ${SW_HIDE}

    nsDialogs::Show
FunctionEnd

; - BrowseClicked -
Function BrowseClicked
    nsDialogs::SelectFolderDialog "Select Bloodshed game folder" "$GamePath"
    Pop $0
    ${If} $0 != "error"
        ${NSD_SetText} $hPathText $0
    ${EndIf}
FunctionEnd

; - Macro: update progress bar -
!macro SetProgress val
    SendMessage $hProgress 0x402 ${val} 0
!macroend

; - Macro: update status text -
!macro SetStatus txt
    SendMessage $hStatus ${WM_SETTEXT} 0 "STR:${txt}"
!macroend

; - MainPageLeave (Install clicked) -
Function MainPageLeave
    ; 1. Collect form values
    ${NSD_GetText}  $hPathText  $GamePath
    ${NSD_GetState} $hBepInExChk $DoInstallBepInEx
    ${NSD_GetState} $hModDllChk  $DoInstallModDll

    ; 2. Validation
    IfFileExists "$GamePath\Bloodshed.exe" +3 0
        MessageBox MB_OK|MB_ICONEXCLAMATION "Bloodshed.exe not found in:$\n$GamePath$\n$\nPlease select the correct game folder."
        Abort

    ${If} $DoInstallBepInEx != ${BST_CHECKED}
    ${AndIf} $DoInstallModDll != ${BST_CHECKED}
        MessageBox MB_OK|MB_ICONEXCLAMATION "Please select at least one item to install."
        Abort
    ${EndIf}

    ; 3. Lock UI
    GetDlgItem $0 $HWNDPARENT 1
    EnableWindow $0 0
    EnableWindow $hBepInExChk 0
    EnableWindow $hModDllChk  0
    EnableWindow $hPathText   0
    EnableWindow $hBrowseBtn  0

    ; - 4. BepInEx -
    ${If} $DoInstallBepInEx == ${BST_CHECKED}
        !insertmacro SetStatus "Checking BepInEx..."

        IfFileExists "$GamePath\BepInEx\core\*.*" BepInExAlreadyInstalled 0

        !insertmacro SetStatus "Downloading BepInEx..."
        nsExec::ExecToStack "powershell -NoProfile -ExecutionPolicy Bypass -Command $\"Invoke-WebRequest -Uri '${BEPINEX_URL}' -OutFile '$TEMP\bepinex_bmti.zip' -UseBasicParsing$\""
        Pop $0
        Pop $1
        ${If} $0 != 0
            MessageBox MB_OK|MB_ICONEXCLAMATION "BepInEx download failed (exit $0):$\n$1$\nCheck your internet connection and try again."
            Abort
        ${EndIf}

        !insertmacro SetStatus "Extracting BepInEx..."
        nsExec::ExecToStack "powershell -NoProfile -ExecutionPolicy Bypass -Command $\"Expand-Archive -LiteralPath '$TEMP\bepinex_bmti.zip' -DestinationPath '$GamePath' -Force$\""
        Pop $0
        Pop $1
        ${If} $0 != 0
            MessageBox MB_OK|MB_ICONEXCLAMATION "BepInEx extraction failed (exit $0):$\n$1"
            Delete "$TEMP\bepinex_bmti.zip"
            Abort
        ${EndIf}
        Delete "$TEMP\bepinex_bmti.zip"
        !insertmacro SetProgress 80
        Goto BepInExDone

        BepInExAlreadyInstalled:
        !insertmacro SetStatus "BepInEx already installed - skipped."
        !insertmacro SetProgress 80

        BepInExDone:
    ${EndIf}

    ; - 5. Mod DLL -
    ${If} $DoInstallModDll == ${BST_CHECKED}
        !insertmacro SetStatus "Downloading mod DLL..."
        FileOpen $0 "$TEMP\bmti_dlmod.ps1" w
        FileWrite $0 "Invoke-WebRequest -Uri '${MOD_DLL_URL}' -OutFile '$TEMP\BloodshedModToolkit.dll' -UseBasicParsing$\n"
        FileClose $0
        nsExec::ExecToStack "powershell -NoProfile -ExecutionPolicy Bypass -File $\"$TEMP\bmti_dlmod.ps1$\""
        Pop $0
        Pop $1
        Delete "$TEMP\bmti_dlmod.ps1"
        ${If} $0 != 0
            MessageBox MB_OK|MB_ICONEXCLAMATION "Mod DLL download failed (exit $0):$\n$1$\nCheck your internet connection and try again."
            Abort
        ${EndIf}

        !insertmacro SetStatus "Installing mod DLL..."
        CreateDirectory "$GamePath\BepInEx\plugins"
        CopyFiles "$TEMP\BloodshedModToolkit.dll" "$GamePath\BepInEx\plugins\"
        Delete "$TEMP\BloodshedModToolkit.dll"
        !insertmacro SetProgress 90
    ${EndIf}

    ; - 6. Done -
    !insertmacro SetProgress 100
    !insertmacro SetStatus "Installation complete!"

    MessageBox MB_OK|MB_ICONINFORMATION "Installation complete!$\n$\nNext steps:$\n  1. Launch Bloodshed once so BepInEx generates interop assemblies.$\n  2. Mods will load automatically on the next launch.$\n$\nGame path: $GamePath"
    Quit
FunctionEnd
