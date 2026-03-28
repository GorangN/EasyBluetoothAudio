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
- Start with a Markdown H1 heading: `# EasyBluetoothAudio v$Version Release Notes`
- Group changes into categories like "New Features", "Bug Fixes", "Improvements" as appropriate
- Format categories as Markdown H2 headings (e.g., `## New Features`)
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

$Uri = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite-preview:generateContent?key=$ApiKey"

$MaxTries = 3
$RetryDelaySeconds = 5
$Attempt = 0
$Success = $false

while ($Attempt -lt $MaxTries -and -not $Success) {
    try {
        $Response = Invoke-RestMethod -Uri $Uri -Method Post -Body $Body -ContentType "application/json; charset=utf-8"
        
        $Notes = $Response.candidates[0].content.parts[0].text
        if (-not $Notes) {
            Write-Warning "Leere Antwort von Gemini. Verwende Standard-Release-Notes."
            Out-Notes "Release v$Version"
        }

        Out-Notes $Notes.Trim()
        $Success = $true
    }
    catch {
        $Exception = $_.Exception
        $StatusCode = $Exception.Response.StatusCode.value__
        
        # Versuche die genaue Fehlermeldung aus dem Body zu lesen
        $ResponseBody = ""
        if ($Exception.Response) {
            $Reader = New-Object System.IO.StreamReader($Exception.Response.GetResponseStream())
            $ResponseBody = $Reader.ReadToEnd()
            $Reader.Close()
        }

        if (($StatusCode -eq 429 -or $StatusCode -eq 503) -and $Attempt -lt ($MaxTries - 1)) {
            Write-Warning "API ($StatusCode) Fehler erreicht. Google sagt: $ResponseBody"
            Write-Warning "Warte $RetryDelaySeconds Sekunden vor Retry..."
            Start-Sleep -Seconds $RetryDelaySeconds
            $Attempt++
        }
        else {
            Write-Warning "Gemini API Fehler ($StatusCode): $ResponseBody"
            Write-Warning "Verwende Standard-Release-Notes."
            Out-Notes "Release v$Version"
        }
    }
}
