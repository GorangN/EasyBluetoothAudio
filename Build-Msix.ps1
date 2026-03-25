<#
.SYNOPSIS
    Packages EasyBluetoothAudio as an MSIX and generates the matching .appinstaller file.

.DESCRIPTION
    This script:
      1. Reads the publish output produced by `dotnet publish`
      2. Generates PNG logo assets from the .ico file using System.Drawing
      3. Substitutes the version into Package.appxmanifest and produces AppxManifest.xml
      4. Packs the MSIX with makeappx.exe (Windows SDK)
      5. Signs the MSIX with a self-signed certificate (created if absent)
      6. Generates a versioned EasyBluetoothAudio.appinstaller file
      7. Writes both artifacts to the Output\ folder

.PARAMETER Version
    3-part semantic version string (e.g. "1.2.3"). A fourth ".0" component is appended
    automatically to satisfy the MSIX 4-part version requirement.

.PARAMETER SkipSigning
    When set, the MSIX is not signed. Useful for CI/CD pipelines that sign separately.

.EXAMPLE
    .\Build-Msix.ps1 -Version "1.4.0"
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string] $Version,

    [switch] $SkipSigning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Constants ────────────────────────────────────────────────────────────────

$PublishDir   = "EasyBluetoothAudio\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
$OutputDir    = "Output"
$StagingDir   = "$OutputDir\_msix_staging"
$MsixOut      = "$OutputDir\EasyBluetoothAudio.msix"
$AppInstOut   = "$OutputDir\EasyBluetoothAudio.appinstaller"
$IcoPath      = "EasyBluetoothAudio\Assets\EasyBluetoothAudio.ico"
$ManifestSrc  = "Package.appxmanifest"
$AppInstSrc   = "EasyBluetoothAudio.appinstaller"
$CertSubject  = "CN=EasyBluetoothAudio"
$MsixVersion  = "$Version.0"   # MSIX requires 4-part version (major.minor.patch.revision)

# ── Helpers ──────────────────────────────────────────────────────────────────

function Find-WindowsSdkTool {
    param([string] $ToolName)
    $base = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    $hit  = Get-ChildItem "$base\10.*\x64\$ToolName" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    if (-not $hit) {
        throw "$ToolName not found under '$base'. Install the Windows 10 SDK."
    }
    return $hit
}

function Save-PngFromIco {
    param([string] $IcoPath, [int] $Size, [string] $OutPath)
    $icon    = New-Object System.Drawing.Icon($IcoPath, $Size, $Size)
    $bitmap  = $icon.ToBitmap()
    $resized = New-Object System.Drawing.Bitmap($Size, $Size)
    $g       = [System.Drawing.Graphics]::FromImage($resized)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($bitmap, 0, 0, $Size, $Size)
    $g.Dispose()
    $bitmap.Dispose()
    $icon.Dispose()
    $resized.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $resized.Dispose()
}

# ── Step 1: Validate publish output ──────────────────────────────────────────

Write-Host "[1/6] Checking publish output..."
if (-not (Test-Path "$PublishDir\EasyBluetoothAudio.exe")) {
    throw "Published EXE not found at '$PublishDir'. Run 'dotnet publish' first."
}

# ── Step 2: Create staging directory ─────────────────────────────────────────

Write-Host "[2/6] Creating MSIX staging directory..."
Remove-Item $StagingDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item "$StagingDir\Assets" -ItemType Directory -Force | Out-Null

# Copy application files
Copy-Item "$PublishDir\EasyBluetoothAudio.exe"          $StagingDir
Copy-Item "$PublishDir\Assets\NotifySound.mp3"  "$StagingDir\Assets\"

# Generate PNG logo assets from the .ico file
Write-Host "  Generating PNG logo assets..."
Add-Type -AssemblyName System.Drawing
Save-PngFromIco $IcoPath 44  "$StagingDir\Assets\Square44x44Logo.png"
Save-PngFromIco $IcoPath 150 "$StagingDir\Assets\Square150x150Logo.png"
Save-PngFromIco $IcoPath 50  "$StagingDir\Assets\StoreLogo.png"

# Substitute version placeholder and write AppxManifest.xml
$manifest = (Get-Content $ManifestSrc -Raw) -replace '\$VERSION\$', $MsixVersion
[System.IO.File]::WriteAllText("$StagingDir\AppxManifest.xml", $manifest, [System.Text.Encoding]::UTF8)

# ── Step 3: Pack MSIX ────────────────────────────────────────────────────────

Write-Host "[3/6] Packing MSIX with makeappx.exe..."
$makeAppx = Find-WindowsSdkTool 'makeappx.exe'
New-Item $OutputDir -ItemType Directory -Force | Out-Null
Remove-Item $MsixOut -Force -ErrorAction SilentlyContinue

& $makeAppx pack /d $StagingDir /p $MsixOut /nv /o
if ($LASTEXITCODE -ne 0) {
    throw "makeappx.exe failed (exit code $LASTEXITCODE)."
}

# ── Step 4: Sign MSIX ────────────────────────────────────────────────────────

if (-not $SkipSigning) {
    Write-Host "[4/6] Signing MSIX with self-signed certificate..."

    $signTool = Find-WindowsSdkTool 'signtool.exe'

    # Reuse existing cert or create a new one (valid 5 years)
    $cert = Get-ChildItem Cert:\CurrentUser\My |
            Where-Object { $_.Subject -eq $CertSubject -and $_.NotAfter -gt (Get-Date) } |
            Select-Object -First 1

    if (-not $cert) {
        Write-Host "  Creating new self-signed certificate ($CertSubject)..."
        $cert = New-SelfSignedCertificate `
            -Subject      $CertSubject `
            -FriendlyName "EasyBluetoothAudio MSIX Signing" `
            -Type         CodeSigning `
            -KeyUsage     DigitalSignature `
            -NotAfter     (Get-Date).AddYears(5) `
            -CertStoreLocation Cert:\CurrentUser\My
        Write-Host "  Certificate thumbprint: $($cert.Thumbprint)"
        Write-Host ""
        Write-Host "  NOTE: To install without a security warning, export this certificate"
        Write-Host "  and import it into 'Trusted People' on the target machine:"
        Write-Host "    certmgr.msc → Trusted People → Certificates → Import"
        Write-Host "  Or run: Export-Certificate -Cert `$cert -FilePath Output\EasyBluetoothAudio.cer"
        Write-Host ""
    }

    & $signTool sign /fd SHA256 /sha1 $cert.Thumbprint $MsixOut
    if ($LASTEXITCODE -ne 0) {
        throw "signtool.exe failed (exit code $LASTEXITCODE)."
    }

    # Export the public cert alongside the MSIX so users can install it easily
    $certOut = "$OutputDir\EasyBluetoothAudio.cer"
    Export-Certificate -Cert $cert -FilePath $certOut -Force | Out-Null
    Write-Host "  Certificate exported to: $certOut"
} else {
    Write-Host "[4/6] Skipping signing (SkipSigning flag set)."
}

# ── Step 5: Generate .appinstaller ───────────────────────────────────────────

Write-Host "[5/6] Generating AppInstaller file..."
$appInst = (Get-Content $AppInstSrc -Raw) -replace '\$VERSION\$', $MsixVersion
[System.IO.File]::WriteAllText($AppInstOut, $appInst, [System.Text.Encoding]::UTF8)

# ── Step 6: Cleanup staging ──────────────────────────────────────────────────

Write-Host "[6/6] Cleaning up staging directory..."
Remove-Item $StagingDir -Recurse -Force

# ── Done ─────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "MSIX build complete:"
Write-Host "  Package:     $MsixOut"
Write-Host "  AppInstaller: $AppInstOut"
if (-not $SkipSigning) {
    Write-Host "  Certificate:  $OutputDir\EasyBluetoothAudio.cer"
}
