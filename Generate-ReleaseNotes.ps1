param (
    [Parameter(Mandatory)][string]$Version,
    [string]$PreviousTag,
    [Parameter(Mandatory)][string]$OutputPath
)

function Out-Notes([string]$Text) {
    Set-Content -Path $OutputPath -Value $Text -Encoding UTF8
    exit 0
}

# --- 1. API Key prüfen ---
# Read directly from User environment to avoid needing a terminal restart
$ApiKey = [Environment]::GetEnvironmentVariable("GEMINI_API_KEY", "User")
if (-not $ApiKey) {
    # If not in User scope, try Process scope just in case it was set there
    $ApiKey = $env:GEMINI_API_KEY
}

if (-not $ApiKey) {
    Write-Warning "GEMINI_API_KEY ist nicht gesetzt. Verwende Standard-Release-Notes."
    Out-Notes "Release v$Version"
}

# --- 2. Vorherigen Tag ermitteln ---
if (-not $PreviousTag) {
    $PreviousTag = git describe --tags --abbrev=0 "v$Version^" 2>$null
    if (-not $PreviousTag) {
        Write-Warning "Kein vorheriger Tag gefunden. Verwende alle Commits."
        $CommitLog = git log "v$Version" --oneline 2>$null
    }
    else {
        $CommitLog = git log "$PreviousTag..v$Version" --oneline 2>$null
    }
}
else {
    $CommitLog = git log "$PreviousTag..v$Version" --oneline 2>$null
}

if (-not $CommitLog) {
    Write-Warning "Keine Commits gefunden. Verwende Standard-Release-Notes."
    Out-Notes "Release v$Version"
}

$CommitText = $CommitLog -join "`n"

# --- 3. Prompt bauen ---
$Prompt = @"
You are a release notes writer for the open-source app "EasyBluetoothAudio" (a Windows WPF tray app for easy Bluetooth audio device management).

Given the following git commits between $PreviousTag and v$Version, write concise, user-friendly release notes in English.

Rules:
- Group changes into categories like "New Features", "Bug Fixes", "Improvements" as appropriate
- Skip merge commits and trivial changes (typos, formatting)
- Use bullet points with short descriptions
- Do NOT include commit hashes
- Keep it concise, max 15 lines
- Do NOT wrap in markdown code blocks

Commits:
$CommitText
"@

# --- 4. Gemini API aufrufen ---
$Body = @{
    contents = @(@{
            parts = @(@{ text = $Prompt })
        })
} | ConvertTo-Json -Depth 5

$Uri = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=$ApiKey"

try {
    $Response = Invoke-RestMethod -Uri $Uri -Method Post -Body $Body -ContentType "application/json; charset=utf-8"
    $Notes = $Response.candidates[0].content.parts[0].text

    if (-not $Notes) {
        Write-Warning "Leere Antwort von Gemini. Verwende Standard-Release-Notes."
        Out-Notes "Release v$Version"
    }

    Out-Notes $Notes.Trim()
}
catch {
    Write-Warning "Gemini API Fehler: $_"
    Write-Warning "Verwende Standard-Release-Notes."
    Out-Notes "Release v$Version"
}
