param ([string]$Type = "patch")

$CurrentVersion = $(git describe --tags --abbrev=0 2>$null)
if (-not $CurrentVersion) { 
    $CurrentVersion = "0.0.0-dev" 
}
$v = [version]($CurrentVersion -replace '^v', '')

$Major, $Minor, $Patch = $v.Major, $v.Minor, $v.Build

switch ($Type) {
    "major" { $Major++; $Minor = 0; $Patch = 0 }
    "minor" { $Minor++; $Patch = 0 }
    "patch" { $Patch++ }
}
return "$Major.$Minor.$Patch"