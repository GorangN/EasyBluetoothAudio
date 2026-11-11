@echo off
setlocal
pushd "%~dp0"

set "TYPE=%~1"
if "%TYPE%"=="" set "TYPE=patch"
if /i "%TYPE%"=="test" goto :do_test_build
if /i "%TYPE%"=="patch" goto :dispatch
if /i "%TYPE%"=="minor" goto :dispatch
if /i "%TYPE%"=="major" goto :dispatch
if /i "%TYPE%"=="pre" goto :dispatch
echo [ERROR] Usage: release.bat [patch^|minor^|major^|pre^|test]
popd
exit /b 1

:dispatch
echo Starting the GitHub release workflow for main (%TYPE%)...
gh workflow run release.yml --repo GorangN/EasyBluetoothAudio --ref main -f "version_type=%TYPE%"
if ERRORLEVEL 1 (
    echo [ERROR] Could not start the workflow. Check gh authentication and that release.yml exists on main.
    popd
    exit /b 1
)
echo Track the release: https://github.com/GorangN/EasyBluetoothAudio/actions/workflows/release.yml
popd
exit /b 0

:do_test_build
set "NEXT_VER="
for /f "delims=" %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -File .\deploy\Get-NextVersion.ps1 -Type patch') do set "NEXT_VER=%%i"
if not defined NEXT_VER (
    echo [ERROR] Could not calculate the test version.
    popd
    exit /b 1
)
set "MinVerVersionOverride=%NEXT_VER%"
set "TEST_APP_NAME=EasyBluetoothAudio Test Installer v%NEXT_VER%"

echo === Building %TEST_APP_NAME% ===
dotnet restore --nologo -v q
if ERRORLEVEL 1 (
    echo [ERROR] dotnet restore failed.
    popd
    exit /b 1
)
dotnet publish EasyBluetoothAudio\EasyBluetoothAudio.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --nologo -v q
if ERRORLEVEL 1 (
    echo [ERROR] dotnet publish failed.
    popd
    exit /b 1
)

set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\iscc.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\iscc.exe"
if exist "%ProgramFiles%\Inno Setup 6\iscc.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\iscc.exe"
if "%ISCC%"=="" (
    echo [ERROR] Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php
    popd
    exit /b 1
)
"%ISCC%" /Q /dMyAppVersion=%NEXT_VER% "/dMyAppName=%TEST_APP_NAME%" "/dMyAppOutputBaseFilename=%TEST_APP_NAME%" "deploy\EasyBluetoothAudioInstaller.iss"
if ERRORLEVEL 1 (
    echo [ERROR] Inno Setup compilation failed.
    popd
    exit /b 1
)

echo === Test Installer built successfully ===
explorer "%~dp0Output"
popd
exit /b 0
