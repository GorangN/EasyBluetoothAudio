@echo off
setlocal

:: 1. Version bestimmen
set TYPE=%1
if "%TYPE%"=="" set TYPE=patch
for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File .\Get-NextVersion.ps1 -Type %TYPE%') do set NEXT_VER=%%i

echo === Starte Release-Prozess fuer v%NEXT_VER% ===

:: 2. Git Tagging & Push
echo [1/4] Erstelle Git Tag...
git tag -a v%NEXT_VER% -m "Release version %NEXT_VER%"
git push origin v%NEXT_VER%

:: 3. Dotnet Publish
echo [2/4] Kompiliere App (dotnet publish)...
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

:: 4. Inno Setup Compiler
echo [3/4] Erstelle Installer mit Inno Setup...
:: Stelle sicher, dass iscc in deinem System-PATH ist
iscc /dMyAppVersion=%NEXT_VER% "EasyBluetoothAudioInstaller.iss"

echo [4/4] Fertig!
echo === Release v%NEXT_VER% erfolgreich erstellt ===
pause