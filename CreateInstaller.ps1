# ============================================================
# ETS Reminder - Create Distributable Installer
# ============================================================
# Creates a ready-to-send zip file containing:
#   1. Self-contained single-file ETS Reminder.exe (no .NET needed)
#   2. Install.bat - one-click installer for the recipient
#
# The recipient just extracts the zip and double-clicks Install.bat
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File CreateInstaller.ps1
# ============================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectDir = Join-Path $scriptDir "ETS reminder"
$projectFile = Join-Path $projectDir "ETS reminder.csproj"
$publishDir = Join-Path $scriptDir "installer-build"
$outputDir = Join-Path $scriptDir "installer-output"
$exeName = "ETS reminder.exe"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ETS Reminder - Installer Creator" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
New-Item -Path $outputDir -ItemType Directory -Force | Out-Null

# Step 2: Publish self-contained single file
Write-Host "[1/3] Building self-contained single-file exe..." -ForegroundColor Yellow

dotnet publish $projectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed." -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $publishDir $exeName
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Published exe not found." -ForegroundColor Red
    exit 1
}

$size = [math]::Round((Get-Item $exePath).Length / 1MB, 1)
Write-Host "  Build successful ($size MB)" -ForegroundColor Green

# Step 3: Copy exe to output
Write-Host "[2/3] Preparing installer package..." -ForegroundColor Yellow
Copy-Item $exePath $outputDir

# Create Install.bat
$installBat = @'
@echo off
title ETS Reminder - Setup
color 0E
echo.
echo   ============================================
echo   #                                          #
echo   #        ETS Reminder - Setup Wizard        #
echo   #                                          #
echo   ============================================
echo.
echo   Welcome to the ETS Reminder installer!
echo   This will install ETS Reminder on your PC.
echo.
echo   What will be installed:
echo     - ETS Reminder application
echo     - Auto-start on Windows login
echo.
echo   Install location:
echo     %LOCALAPPDATA%\ETS Reminder
echo.
echo   ============================================
echo.
set /p BEGIN="  Press ENTER to begin installation or type Q to quit: "
if /I "%BEGIN%"=="Q" (
    echo.
    echo   Installation cancelled.
    timeout /t 2 >nul
    exit /b 0
)

set "INSTALL_DIR=%LOCALAPPDATA%\ETS Reminder"
set "EXE_NAME=ETS reminder.exe"
set "SCRIPT_DIR=%~dp0"

echo.
echo   --------------------------------------------
echo   Step 1/4: Preparing installation...
echo   --------------------------------------------

:: Stop running instance
taskkill /IM "%EXE_NAME%" /F >nul 2>&1
timeout /t 1 /nobreak >nul

:: Create install directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy exe
copy /Y "%SCRIPT_DIR%%EXE_NAME%" "%INSTALL_DIR%\%EXE_NAME%" >nul
if errorlevel 1 (
    color 0C
    echo.
    echo   ERROR: Failed to copy files. Try running as Administrator.
    echo.
    pause
    exit /b 1
)
echo   [OK] Application files installed.

echo.
echo   --------------------------------------------
echo   Step 2/4: Desktop Shortcut
echo   --------------------------------------------
echo.
set /p SHORTCUT="  Create a desktop shortcut? (Y/N): "
if /I "%SHORTCUT%"=="Y" (
    echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\ets_shortcut.vbs"
    echo sLinkFile = oWS.SpecialFolders("Desktop") ^& "\ETS Reminder.lnk" >> "%TEMP%\ets_shortcut.vbs"
    echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\ets_shortcut.vbs"
    echo oLink.TargetPath = "%INSTALL_DIR%\%EXE_NAME%" >> "%TEMP%\ets_shortcut.vbs"
    echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\ets_shortcut.vbs"
    echo oLink.Description = "ETS Daily Report Reminder" >> "%TEMP%\ets_shortcut.vbs"
    echo oLink.IconLocation = "%INSTALL_DIR%\%EXE_NAME%,0" >> "%TEMP%\ets_shortcut.vbs"
    echo oLink.Save >> "%TEMP%\ets_shortcut.vbs"
    cscript /nologo "%TEMP%\ets_shortcut.vbs"
    del "%TEMP%\ets_shortcut.vbs"
    echo   [OK] Desktop shortcut created.
) else (
    echo   [--] Skipped desktop shortcut.
)

echo.
echo   --------------------------------------------
echo   Step 3/4: Windows Startup
echo   --------------------------------------------
echo.
set /p STARTUP="  Start ETS Reminder automatically on login? (Y/N): "
if /I "%STARTUP%"=="Y" (
    set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
    echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\ets_startup.vbs"
    echo sLinkFile = "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\ETS Reminder.lnk" >> "%TEMP%\ets_startup.vbs"
    echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\ets_startup.vbs"
    echo oLink.TargetPath = "%INSTALL_DIR%\%EXE_NAME%" >> "%TEMP%\ets_startup.vbs"
    echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\ets_startup.vbs"
    echo oLink.Description = "ETS Daily Report Reminder" >> "%TEMP%\ets_startup.vbs"
    echo oLink.IconLocation = "%INSTALL_DIR%\%EXE_NAME%,0" >> "%TEMP%\ets_startup.vbs"
    echo oLink.Save >> "%TEMP%\ets_startup.vbs"
    cscript /nologo "%TEMP%\ets_startup.vbs"
    del "%TEMP%\ets_startup.vbs"
    echo   [OK] Added to Windows startup.
) else (
    echo   [--] Skipped auto-start.
)

echo.
echo   --------------------------------------------
echo   Step 4/5: Registering application...
echo   --------------------------------------------
echo.

set "REG_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\ETSReminder"
reg add "%REG_KEY%" /v "DisplayName" /t REG_SZ /d "ETS Reminder" /f >nul
reg add "%REG_KEY%" /v "DisplayIcon" /t REG_SZ /d "%INSTALL_DIR%\%EXE_NAME%,0" /f >nul
reg add "%REG_KEY%" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f >nul
reg add "%REG_KEY%" /v "Publisher" /t REG_SZ /d "Nemanja Grokanic" /f >nul
reg add "%REG_KEY%" /v "DisplayVersion" /t REG_SZ /d "1.0.0" /f >nul
reg add "%REG_KEY%" /v "NoModify" /t REG_DWORD /d 1 /f >nul
reg add "%REG_KEY%" /v "NoRepair" /t REG_DWORD /d 1 /f >nul
echo   [OK] Registered in Windows Apps.

echo.
echo   --------------------------------------------
echo   Step 5/5: Complete!
echo   --------------------------------------------
echo.
color 0A
echo   ============================================
echo   #                                          #
echo   #    Installation completed successfully!   #
echo   #                                          #
echo   ============================================
echo.
echo   Location:  %INSTALL_DIR%
if /I "%SHORTCUT%"=="Y" echo   Shortcut:  Desktop
if /I "%STARTUP%"=="Y" echo   Startup:   Enabled (runs on login)
echo.
echo   Tip: ETS Reminder runs in the system tray.
echo   Look for the orange ETS icon near the clock.
echo.
echo   --------------------------------------------
echo.
set /p LAUNCH="  Launch ETS Reminder now? (Y/N): "
if /I "%LAUNCH%"=="Y" start "" "%INSTALL_DIR%\%EXE_NAME%"
if /I "%LAUNCH%"=="" start "" "%INSTALL_DIR%\%EXE_NAME%"

echo.
echo   Thank you for installing ETS Reminder!
echo.
timeout /t 3 >nul
'@

$installBat | Out-File -FilePath (Join-Path $outputDir "Install.bat") -Encoding ASCII

# Create Uninstall.bat
$uninstallBat = @'
@echo off
title ETS Reminder - Uninstaller
echo.
echo ============================================
echo   ETS Reminder - Uninstaller
echo ============================================
echo.

set "INSTALL_DIR=%LOCALAPPDATA%\ETS Reminder"
set "EXE_NAME=ETS reminder.exe"

set /p CONFIRM="Are you sure you want to uninstall ETS Reminder? (Y/N): "
if /I not "%CONFIRM%"=="Y" (
    echo Cancelled.
    timeout /t 2 >nul
    exit /b 0
)

:: Stop running instance
taskkill /IM "%EXE_NAME%" /F >nul 2>&1
timeout /t 1 /nobreak >nul

:: Remove install directory
if exist "%INSTALL_DIR%" (
    rmdir /s /q "%INSTALL_DIR%"
    echo [OK] Files removed.
) else (
    echo [--] Install directory not found.
)

:: Remove Desktop shortcut
set "DESKTOP=%USERPROFILE%\Desktop\ETS Reminder.lnk"
if exist "%DESKTOP%" (
    del "%DESKTOP%"
    echo [OK] Desktop shortcut removed.
)

:: Remove Startup shortcut
set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\ETS Reminder.lnk"
if exist "%STARTUP%" (
    del "%STARTUP%"
    echo [OK] Startup entry removed.
)

:: Remove registry entry
set "REG_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\ETSReminder"
reg delete "%REG_KEY%" /f >nul 2>&1
echo [OK] Removed from Windows Apps.

echo.
echo Uninstall complete.
echo Note: Your reports in Documents\ETS Reports are preserved.
echo.
pause
'@

$uninstallBat | Out-File -FilePath (Join-Path $outputDir "Uninstall.bat") -Encoding ASCII

# Step 4: Create ZIP
Write-Host "[3/3] Creating zip file..." -ForegroundColor Yellow

$zipPath = Join-Path $scriptDir "ETS-Reminder-Setup.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Compress-Archive -Path (Join-Path $outputDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "  Created: $zipPath ($zipSize MB)" -ForegroundColor Green

# Cleanup
Remove-Item $publishDir -Recurse -Force
Remove-Item $outputDir -Recurse -Force

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Installer package ready!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  File: ETS-Reminder-Setup.zip" -ForegroundColor White
Write-Host "  Size: $zipSize MB" -ForegroundColor White
Write-Host ""
Write-Host "  Send this zip to your colleague." -ForegroundColor White
Write-Host "  They just extract it and double-click Install.bat" -ForegroundColor White
Write-Host "  No .NET installation needed!" -ForegroundColor White
Write-Host ""
