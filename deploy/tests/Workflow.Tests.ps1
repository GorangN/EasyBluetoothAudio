$ErrorActionPreference = 'Stop'
$workflow = (Get-Content "$PSScriptRoot/../../.github/workflows/release.yml" -Raw).Replace("`r`n", "`n")
if ($workflow -notmatch 'options: \[patch, minor, major, pre, test\]') {
    throw 'Deployment selection must include patch, minor, major, pre and test.'
}
foreach ($stepName in @('Create local release tag', 'Generate release notes', 'Publish GitHub Release')) {
    $guardedStep = "      - name: $stepName`n        if: steps.version.outputs.TYPE != 'test'"
    if (-not $workflow.Replace("`r`n", "`n").Contains($guardedStep)) {
        throw "Test mode must skip '$stepName'."
    }
}
if ($workflow -notmatch "(?m)^        if: steps.version.outputs.TYPE == 'test'$" -or
    $workflow -notmatch 'path: Output/EasyBluetoothAudio Test Installer v\$\{\{ steps.version.outputs.VERSION \}\}\.exe' -or
    -not $workflow.Contains("inputs.version_type || vars.RELEASE_TYPE || 'patch'")) {
    throw 'Test installer download or configurable push deployment is missing.'
}
Write-Host 'PASS: All deployment choices, configurable push mode and tag-free test artifact routing'
$publishMatch = [regex]::Match($workflow, '(?ms)      - name: Publish GitHub Release\r?\n.*?        run: \|\r?\n(?<code>(?:          [^\r\n]*\r?\n|\r?\n)+)')
if (-not $publishMatch.Success) {
    throw 'Could not find the release publication step.'
}

$publishCode = $publishMatch.Groups['code'].Value -replace '(?m)^          ', ''
$tokens = $null
$parseErrors = $null
$null = [Management.Automation.Language.Parser]::ParseInput($publishCode, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors) {
    throw ($parseErrors -join "`n")
}
$publishStep = [scriptblock]::Create(($publishCode -replace 'exit (\$LASTEXITCODE|0)', 'throw "STEP_EXIT:$1"'))

function Test-Publication {
    param(
        [string]$Name,
        [string]$Releases = '[]',
        [string]$FailCommand,
        [string[]]$Expected,
        [string]$Production = 'True',
        [string]$ExpectedExit
    )

    $commands = [Collections.Generic.List[string]]::new()
    function git {
        $command = "git $($args -join ' ')"
        $commands.Add($command)
        $global:LASTEXITCODE = if ($command -eq $FailCommand) { 1 } else { 0 }
    }
    function gh {
        $command = "gh $($args -join ' ')"
        $commands.Add($command)
        $global:LASTEXITCODE = if ($command -eq $FailCommand) { 1 } else { 0 }
        if ($args[0] -eq 'release' -and $args[1] -eq 'list') {
            return $Releases
        }
    }

    $env:RELEASE_VERSION = '2.0.3'
    $env:IS_PRODUCTION = $Production
    $env:RUNNER_TEMP = 'runner-temp'
    $actualExit = ''
    try {
        & $publishStep | Out-Null
    }
    catch {
        if ($_.Exception.Message -notlike 'STEP_EXIT:*') {
            throw
        }
        $actualExit = $_.Exception.Message.Substring('STEP_EXIT:'.Length)
    }
    if ($actualExit -ne $ExpectedExit) {
        throw "${Name}: expected exit '$ExpectedExit', got '$actualExit'."
    }
    if (($commands -join "`n") -cne ($Expected -join "`n")) {
        throw "${Name}: unexpected publication commands:`n$($commands -join "`n")"
    }
    Write-Host "PASS: $Name"
}

$savedEnvironment = @{}
$savedExitCode = $global:LASTEXITCODE
foreach ($key in @('RELEASE_VERSION', 'IS_PRODUCTION', 'RUNNER_TEMP')) {
    $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
try {
    $list = 'gh release list --limit 1000 --json tagName,isDraft,isPrerelease'
    $push = 'git push origin refs/tags/v2.0.3'
    $create = 'gh release create v2.0.3 --verify-tag --draft --title EasyBluetoothAudio v2.0.3 --notes-file runner-temp/release_notes.md'
    $upload = 'gh release upload v2.0.3 Output/EasyBluetoothAudioSetup.exe --clobber'
    $publish = 'gh release edit v2.0.3 --draft=false'
    Test-Publication -Name 'Create, upload, then publish' -Expected @($list, $push, $create, $upload, $publish)
    Test-Publication -Name 'Prerelease flag' -Production 'False' -Expected @($list, $push, "$create --prerelease", $upload, $publish)
    Test-Publication -Name 'Resume existing draft' -Releases '[{"tagName":"v2.0.3","isDraft":true}]' -Expected @($list, $push, $upload, $publish)
    Test-Publication -Name 'Published release is untouched' -Releases '[{"tagName":"v2.0.3","isDraft":false}]' -Expected @($list) -ExpectedExit '0'
    Test-Publication -Name 'API failure stops publication' -FailCommand $list -Expected @($list) -ExpectedExit '1'
    Test-Publication -Name 'Tag push failure stops publication' -FailCommand $push -Expected @($list, $push) -ExpectedExit '1'
    Test-Publication -Name 'Draft failure stops upload' -FailCommand $create -Expected @($list, $push, $create) -ExpectedExit '1'
    Test-Publication -Name 'Upload failure leaves draft unpublished' -FailCommand $upload -Expected @($list, $push, $create, $upload) -ExpectedExit '1'
    Test-Publication -Name 'Publish failure is reported' -FailCommand $publish -Expected @($list, $push, $create, $upload, $publish) -ExpectedExit '1'
}
finally {
    $global:LASTEXITCODE = $savedExitCode
    foreach ($key in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key], 'Process')
    }
}
