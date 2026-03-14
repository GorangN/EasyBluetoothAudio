@echo off
setlocal

:: Define colors
for /F "delims=#" %%E in ('"prompt #$E# & for %%a in (1) do rem"') do set "ESC=%%E"
set "GREEN=%ESC%[92m"
set "YELLOW=%ESC%[93m"
set "CYAN=%ESC%[96m"
set "RED=%ESC%[91m"
set "RESET=%ESC%[0m"

:: Synchronize local tags with remote (deletes local tags that were deleted on GitHub)
git fetch --prune --prune-tags >nul 2>&1

:: 1. Determine version
set TYPE=%1
if "%TYPE%"=="" set TYPE=patch
for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File .\Get-NextVersion.ps1 -Type %TYPE%') do set NEXT_VER=%%i

echo %CYAN%=== Starting release process for v%NEXT_VER% ===%RESET%

:: 2. Git Tagging & Push
echo %YELLOW%[1/6] Creating Git Tag...%RESET%
git tag -a v%NEXT_VER% -m "Release version %NEXT_VER%"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Failed to create git tag v%NEXT_VER%. Aborting.%RESET%
    pause & exit /b 1
)
git push origin v%NEXT_VER% >nul 2>&1

:: 3. Dotnet Publish
echo %YELLOW%[2/6] Compiling App (dotnet publish)...%RESET%
dotnet restore --nologo -v q
if ERRORLEVEL 1 (
    echo %RED%[ERROR] dotnet restore failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)
dotnet publish EasyBluetoothAudio\EasyBluetoothAudio.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --nologo -v q
if ERRORLEVEL 1 (
    echo %RED%[ERROR] dotnet publish failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)

:: 4. Inno Setup Compiler
echo %YELLOW%[3/6] Creating Installer with Inno Setup...%RESET%
set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\iscc.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\iscc.exe"
if exist "%ProgramFiles%\Inno Setup 6\iscc.exe"      set "ISCC=%ProgramFiles%\Inno Setup 6\iscc.exe"
if "%ISCC%"=="" (
    echo %RED%[ERROR] Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)
"%ISCC%" /Q /dMyAppVersion=%NEXT_VER% "EasyBluetoothAudioInstaller.iss"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Inno Setup compilation failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)

:: 5. Generate AI Release Notes
echo %YELLOW%[4/6] Generating Release Notes with AI...%RESET%
powershell -ExecutionPolicy Bypass -File .\Generate-ReleaseNotes.ps1 -Version %NEXT_VER% -OutputPath "%TEMP%\release_notes.md"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Release notes generation failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)

:: 6. Create GitHub Release & Upload Installer
echo %YELLOW%[5/6] Creating GitHub Release and uploading installer...%RESET%
gh release create v%NEXT_VER% Output\EasyBluetoothAudioSetup.exe --title "EasyBluetoothAudio v%NEXT_VER%" --notes-file "%TEMP%\release_notes.md"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] GitHub release creation failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)

echo %GREEN%[6/6] Done!%RESET%
echo %GREEN%=== Release v%NEXT_VER% successfully created and uploaded to GitHub ===%RESET%
pause
