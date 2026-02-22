# Deployment

## Manually

```powershell
git tag -a v0.0.0
git push origin v0.0.0

dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

$TAG = git describe --tags --abbrev=0
iscc /dMyAppVersion=$TAG "EasyBluetoothAudioInstaller.iss"
```

## Using Release Script

```powershell
release.bat patch  #(für kleine Fixes) 0.0.1
release.bat minor  #(für neue Features) 0.1.0
release.bat major  #(für große Sprünge) 1.0.0
```

## Delete Tag and Push (if needed)

```powershell
#See git tag for available tags
git describe --tags --abbrev=0
git tag -l --sort=-v:refname

#Delete tag and push
git tag -d v0.0.0
git push origin --delete v0.0.0
```

---

## <img src="https://api.iconify.design/tabler:terminal-2.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Development

### Prerequisites

* Visual Studio 2022 (Preview) or VS Code.
* .NET 10 SDK installed.

### Build Instructions

The application is configured for **Self-Contained** deployment (no client-side runtime required).

```powershell
# Restore dependencies
dotnet restore

# Build for release (Single File)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
