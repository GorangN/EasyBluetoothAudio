[CmdletBinding()]
param (
    [ValidateSet('patch', 'minor', 'major', 'pre', 'test')]
    [string]$Type = 'patch',
    [ValidatePattern('^[0-9]*$')]
    [string]$ReleaseRunId
)

$ErrorActionPreference = 'Stop'
$releaseTagPattern = '^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'

if ($ReleaseRunId -and $Type -ne 'test') {
    $headTags = git for-each-ref --points-at=HEAD '--format=%(refname:strip=2)|%(contents:subject)' refs/tags
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not read tags at HEAD.'
    }
    $headVersion = $headTags | ForEach-Object {
        $tag, $subject = $_ -split '\|', 2
        if ($tag -cmatch $releaseTagPattern -and $subject -clike "* (GitHub Actions run $ReleaseRunId)") {
            [version]$tag.Substring(1)
        }
    } | Sort-Object -Descending | Select-Object -First 1
    if ($headVersion) {
        return $headVersion.ToString()
    }
}

$tags = git tag --list
if ($LASTEXITCODE -ne 0) {
    throw 'Could not read repository tags.'
}
$currentVersion = $tags | Where-Object { $_ -cmatch $releaseTagPattern } |
    ForEach-Object { [version]$_.Substring(1) } | Sort-Object -Descending | Select-Object -First 1
if (-not $currentVersion) {
    $currentVersion = [version]'0.0.0'
}

$major, $minor, $patch = $currentVersion.Major, $currentVersion.Minor, $currentVersion.Build
switch ($Type) {
    'major' { $major++; $minor = 0; $patch = 0 }
    'minor' { $minor++; $patch = 0 }
    default { $patch++ }
}
return "$major.$minor.$patch"
