@echo off
setlocal

:: 1. Version bestimmen
set TYPE=%1
if "%TYPE%"=="" set TYPE=patch
for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File .\Get-NextVersion.ps1 -Type %TYPE%') do set NEXT_VER=%%i

echo === Starte Release-Prozess fuer v%NEXT_VER% ===

:: 2. Git Tagging & Push
echo [1/5] Erstelle Git Tag...
git tag -a v%NEXT_VER% -m "Release version %NEXT_VER%"
git push origin v%NEXT_VER%

:: 3. Dotnet Publish
echo [2/5] Kompiliere App (dotnet publish)...
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

:: 4. Inno Setup Compiler
echo [3/5] Erstelle Installer mit Inno Setup...
:: Stelle sicher, dass iscc in deinem System-PATH ist
iscc /dMyAppVersion=%NEXT_VER% "EasyBluetoothAudioInstaller.iss"

:: 5. GitHub Release erstellen & Installer hochladen
echo [4/5] Erstelle GitHub Release und lade Installer hoch...
gh release create v%NEXT_VER% Output\EasyBluetoothAudioSetup.exe --title "v%NEXT_VER%" --generate-notes

echo [5/5] Fertig!
echo === Release v%NEXT_VER% erfolgreich erstellt und auf GitHub hochgeladen ===
pause