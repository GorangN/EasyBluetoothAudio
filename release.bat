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
if /i "%TYPE%"=="test" goto :do_test_build
for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File .\Get-NextVersion.ps1 -Type %TYPE%') do set NEXT_VER=%%i

echo %CYAN%=== Starting release process for v%NEXT_VER% ===%RESET%

:: 1b. Get previous release tag (to delete later)
for /f "delims=" %%i in ('gh release list --limit 1 --json tagName --jq ".[0].tagName" 2^>nul') do set PREV_TAG=%%i

:: 2. Git Tagging & Push
echo %YELLOW%[1/7] Creating Git Tag...%RESET%
git tag -a v%NEXT_VER% -m "Release version %NEXT_VER%"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Failed to create git tag v%NEXT_VER%. Aborting.%RESET%
    pause & exit /b 1
)
git push origin v%NEXT_VER% >nul 2>&1

:: 3. Dotnet Publish
echo %YELLOW%[2/7] Compiling App (dotnet publish)...%RESET%
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
echo %YELLOW%[3/7] Creating Installer with Inno Setup...%RESET%
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
echo %YELLOW%[4/7] Generating Release Notes with AI...%RESET%
powershell -ExecutionPolicy Bypass -File .\Generate-ReleaseNotes.ps1 -Version %NEXT_VER% -OutputPath "%TEMP%\release_notes.md"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Release notes generation failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)

:: 6. Create GitHub Release & Upload Installer
echo %YELLOW%[5/7] Creating GitHub Release and uploading installer...%RESET%
gh release create v%NEXT_VER% Output\EasyBluetoothAudioSetup.exe --title "EasyBluetoothAudio v%NEXT_VER%" --notes-file "%TEMP%\release_notes.md"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] GitHub release creation failed. Aborting.%RESET%
    git tag -d v%NEXT_VER% >nul 2>&1
    pause & exit /b 1
)

:: 7. Delete previous GitHub Release
echo %YELLOW%[6/7] Cleaning up previous release...%RESET%
if not "%PREV_TAG%"=="" (
    gh release delete %PREV_TAG% --yes >nul 2>&1
    if ERRORLEVEL 1 (
        echo %YELLOW%[WARN] Could not delete previous release %PREV_TAG% (may not exist).%RESET%
    ) else (
        echo %GREEN%Deleted previous release %PREV_TAG%.%RESET%
    )
) else (
    echo %YELLOW%No previous release found to delete.%RESET%
)

echo %GREEN%=== Release v%NEXT_VER% successfully created and uploaded to GitHub ===%RESET%
explorer "%~dp0Output"
pause
goto :eof

:do_test_build
:: Determine what the next patch version would be (for naming only — no GitHub interaction)
for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File .\Get-NextVersion.ps1 -Type patch') do set NEXT_VER=%%i

set "TEST_TAG=v%NEXT_VER%"
set "TEST_APP_NAME=EasyBluetoothAudio Test Installer %TEST_TAG%"
set "TEST_OUTPUT_FILE=%TEST_APP_NAME%.exe"
set "TEST_OUTPUT_PATH=%~dp0Output\%TEST_OUTPUT_FILE%"

echo %CYAN%=== Building Test Installer: %TEST_APP_NAME% ===%RESET%

:: Create a temporary local-only tag (used for versioning; deleted after build)
echo %YELLOW%[1/3] Creating temporary local tag %TEST_TAG%...%RESET%
git tag -d %TEST_TAG% >nul 2>&1
git tag -a %TEST_TAG% -m "Temporary tag for test build %TEST_TAG%"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Failed to create local tag %TEST_TAG%. Aborting.%RESET%
    pause & exit /b 1
)

:: Dotnet Publish
echo %YELLOW%[2/3] Compiling App (dotnet publish)...%RESET%
dotnet restore --nologo -v q
if ERRORLEVEL 1 (
    echo %RED%[ERROR] dotnet restore failed. Aborting.%RESET%
    git tag -d %TEST_TAG% >nul 2>&1
    pause & exit /b 1
)
dotnet publish EasyBluetoothAudio\EasyBluetoothAudio.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --nologo -v q
if ERRORLEVEL 1 (
    echo %RED%[ERROR] dotnet publish failed. Aborting.%RESET%
    git tag -d %TEST_TAG% >nul 2>&1
    pause & exit /b 1
)

:: Inno Setup — build with custom AppName and output filename
echo %YELLOW%[3/3] Creating Test Installer with Inno Setup...%RESET%
set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\iscc.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\iscc.exe"
if exist "%ProgramFiles%\Inno Setup 6\iscc.exe"      set "ISCC=%ProgramFiles%\Inno Setup 6\iscc.exe"
if "%ISCC%"=="" (
    echo %RED%[ERROR] Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php%RESET%
    git tag -d %TEST_TAG% >nul 2>&1
    pause & exit /b 1
)
"%ISCC%" /Q /dMyAppVersion=%NEXT_VER% "/dMyAppName=%TEST_APP_NAME%" "/dMyAppOutputBaseFilename=%TEST_APP_NAME%" "EasyBluetoothAudioInstaller.iss"
if ERRORLEVEL 1 (
    echo %RED%[ERROR] Inno Setup compilation failed. Aborting.%RESET%
    git tag -d %TEST_TAG% >nul 2>&1
    pause & exit /b 1
)

:: Remove the temporary local tag — no trace left in Git
git tag -d %TEST_TAG% >nul 2>&1

echo %GREEN%=== Test Installer built successfully ===%RESET%
explorer "%~dp0Output"
pause
goto :eof
