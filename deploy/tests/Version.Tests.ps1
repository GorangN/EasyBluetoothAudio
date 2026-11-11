$ErrorActionPreference = 'Stop'
$versionScript = (Resolve-Path "$PSScriptRoot/../Get-NextVersion.ps1").Path
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "eba-release-tests-$([guid]::NewGuid())"
$null = New-Item -ItemType Directory -Path $testRoot
$failures = [Collections.Generic.List[string]]::new()

function Invoke-Git {
    param([string[]]$Arguments)
    & git @Arguments 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "git $Arguments failed with exit code $LASTEXITCODE."
    }
}

function Assert-Version {
    param([string]$Name, [string]$Expected, [hashtable]$Arguments = @{})
    try {
        $actual = & $versionScript @Arguments
        if ($actual -ne $Expected) {
            throw "Expected '$Expected', got '$actual'."
        }
        Write-Host "PASS: $Name"
    }
    catch {
        $failures.Add("${Name}: $_")
    }
}

Push-Location $testRoot
try {
    Invoke-Git @('init', '--quiet')
    Invoke-Git @('config', 'user.name', 'Release Test')
    Invoke-Git @('config', 'user.email', 'release-test@example.com')
    Invoke-Git @('commit', '--allow-empty', '-m', 'initial')
    Assert-Version 'First patch without tags' '0.0.1'
    try {
        $null = & $versionScript -Type invalid
        $failures.Add('Invalid bump input was accepted.')
    }
    catch [System.Management.Automation.ParameterBindingException] {
        Write-Host 'PASS: Invalid bump input rejected'
    }
    Invoke-Git @('tag', 'v2.0.0')
    Invoke-Git @('checkout', '-b', 'other-release')
    Invoke-Git @('commit', '--allow-empty', '-m', 'other branch release')
    Invoke-Git @('tag', 'v2.0.1')
    Invoke-Git @('tag', 'v2.0.2')
    Invoke-Git @('checkout', '-')
    Assert-Version 'Unreachable occupied tags are reserved' '2.0.3'
    Invoke-Git @('tag', 'v2.0.9')
    Invoke-Git @('tag', 'v2.0.10')
    Invoke-Git @('tag', 'unrelated-99.0.0')
    Invoke-Git @('tag', 'v99.0')
    Assert-Version 'Numeric ordering and unrelated tags' '2.0.11'
    Assert-Version 'Minor increment' '2.1.0' @{ Type = 'minor' }
    Assert-Version 'Major increment' '3.0.0' @{ Type = 'major' }
    Assert-Version 'Prerelease reserves the next patch' '2.0.11' @{ Type = 'pre' }
    Assert-Version 'New manual minor on tagged commit' '2.1.0' @{ Type = 'minor'; ReleaseRunId = '100' }
    Assert-Version 'New manual major on tagged commit' '3.0.0' @{ Type = 'major'; ReleaseRunId = '100' }
    Assert-Version 'Test build uses next patch' '2.0.11' @{ Type = 'test'; ReleaseRunId = '100' }
    Invoke-Git @('tag', '-a', 'v2.0.11', '-m', 'Release version 2.0.11 (GitHub Actions run 100)')
    Assert-Version 'Same workflow retry reuses its tag' '2.0.11' @{ ReleaseRunId = '100' }
    Assert-Version 'Another workflow receives a new version' '2.0.12' @{ ReleaseRunId = '200' }
    Invoke-Git @('tag', '-a', 'v2.1.0', '-m', 'Release version 2.1.0 (GitHub Actions run 200)')
    Assert-Version 'Retry after later release retains its own version' '2.0.11' @{ ReleaseRunId = '100' }
    Assert-Version 'Separate minor release retry retains its own version' '2.1.0' @{ Type = 'minor'; ReleaseRunId = '200' }
    Invoke-Git @('commit', '--allow-empty', '-m', 'new push')
    Assert-Version 'New commit receives a new version' '2.1.1' @{ ReleaseRunId = '300' }
}
finally {
    Pop-Location
    if ((Split-Path $testRoot -Parent) -ne [IO.Path]::GetTempPath().TrimEnd('\', '/')) {
        throw "Unexpected temporary test path: $testRoot"
    }
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}

if ($failures.Count -gt 0) {
    throw ($failures -join "`n")
}
