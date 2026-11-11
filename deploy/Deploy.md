# Deployment

## Automatic releases with GitHub Actions

Every push to `main` starts the [Release workflow](https://github.com/GorangN/EasyBluetoothAudio/actions/workflows/release.yml). The default is a stable patch release. To choose a different deployment for pushes, set the repository variable `RELEASE_TYPE` under **Settings > Secrets and variables > Actions > Variables** to one of the options below. Pull requests and pushes to other branches do not publish releases.

| Option | Result |
| --- | --- |
| `patch` | Stable release; increment the patch number, e.g. 2.0.3 → 2.0.4. |
| `minor` | Stable release; increment minor and reset patch, e.g. 2.0.3 → 2.1.0. |
| `major` | Stable release; increment major and reset minor/patch, e.g. 2.0.3 → 3.0.0. |
| `pre` | Next patch number, published as a GitHub prerelease; ignored by the app updater. |
| `test` | Test installer available under the workflow run's **Artifacts**; no Git tag or GitHub release. |

The workflow runs on Windows with .NET 10 and performs these steps:

1. Fetch all tags and calculate the next version from the highest `vMAJOR.MINOR.PATCH` tag, including tags on other branches and tags used for GitHub prereleases.
2. Restore, build with warnings as errors, and run the tests.
3. Publish the self-contained x64 app with the calculated MinVer version and compile the Inno Setup installer.
4. Generate release notes, push the tag, upload `EasyBluetoothAudioSetup.exe` to a draft release, then publish it.

Deployments run sequentially. Each release tag records the GitHub workflow run ID. A retry of that run reuses its version, resumes an unfinished draft, or leaves its published release intact. A new run honors the selected increment even when the commit already has another release tag. Build/test/installer failures do not publish tags. Previous releases remain available.

The built-in `GITHUB_TOKEN` is granted `contents: write` by the workflow; no personal access token is required. Set the optional repository secret `GEMINI_API_KEY` for AI release notes. Without it, the existing notes generator produces standard notes. MSIX remains disabled until trusted signing is configured.

## Manual releases

In GitHub, select **Actions > Release > Run workflow**, choose `main`, and select `patch`, `minor`, `major`, `pre`, or `test`. This selection overrides `RELEASE_TYPE` for that run. Use **Re-run jobs** to retry an existing release with its original version; **Run workflow** starts a new deployment with the requested increment.

For `test`, the workflow builds `EasyBluetoothAudio Test Installer vX.Y.Z.exe` and uploads it as an artifact named `EasyBluetoothAudio-Test-Installer-vX.Y.Z`. Download it from the completed run's **Artifacts** section. Tagging, release notes and GitHub release publication are skipped.

Alternatively, the batch entry point starts the same remote workflow (requires an authenticated GitHub CLI with repository access):

```bat
release.bat patch
release.bat minor
release.bat major
release.bat pre
```

These commands release the current remote `main`; push local changes first. They return once GitHub accepts the workflow request. Follow the workflow link printed by the script for the release result. Regular pushes to `main` need no batch command. To start a remote test installer from the CLI, use:

```powershell
gh workflow run release.yml --repo GorangN/EasyBluetoothAudio --ref main -f version_type=test
```

## Local test installer

```bat
release.bat test
```

This builds a self-contained test installer under `Output` using the local SDK and Inno Setup 6. It does not create, delete or push Git tags, and does not create a GitHub release.

## Verification

```powershell
pwsh -NoProfile -File deploy/tests/Version.Tests.ps1
pwsh -NoProfile -File deploy/tests/Workflow.Tests.ps1
dotnet restore
dotnet build --no-restore -c Release -warnaserror
dotnet test --no-build -c Release
```
