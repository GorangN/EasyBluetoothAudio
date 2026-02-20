git tag -a v0.0.0
git push origin v0.0.0
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
iscc /DMyAppVersion="0.0.0" -i "EasyBluetoothAudioInstaller.iss"