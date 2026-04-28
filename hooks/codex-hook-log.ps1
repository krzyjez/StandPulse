$ErrorActionPreference = 'Stop'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = $utf8NoBom
[Console]::OutputEncoding = $utf8NoBom
$OutputEncoding = $utf8NoBom

function Add-TextWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [System.Text.Encoding]$Encoding
    )

    for ($attempt = 1; $attempt -le 8; $attempt++) {
        try {
            [System.IO.File]::AppendAllText($Path, $Text, $Encoding)
            return
        }
        catch [System.IO.IOException] {
            if ($attempt -eq 8) {
                throw
            }
            Start-Sleep -Milliseconds (25 * $attempt)
        }
    }
}

function Set-TextWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [System.Text.Encoding]$Encoding
    )

    for ($attempt = 1; $attempt -le 8; $attempt++) {
        try {
            [System.IO.File]::WriteAllText($Path, $Text, $Encoding)
            return
        }
        catch [System.IO.IOException] {
            if ($attempt -eq 8) {
                throw
            }
            Start-Sleep -Milliseconds (25 * $attempt)
        }
    }
}

try {
    $inputJson = [Console]::In.ReadToEnd()
    $payload = $null
    $parseError = $null

    if (-not [string]::IsNullOrWhiteSpace($inputJson)) {
        try {
            $payload = $inputJson | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            $parseError = $_.Exception.Message
        }
    }

    $eventName = 'Unknown'
    if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'hook_event_name') {
        $eventName = [string]$payload.hook_event_name
    }

    $logRoot = Join-Path -Path $PSScriptRoot -ChildPath 'logs'
    New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

    $record = [ordered]@{
        loggedAt = (Get-Date).ToString('o')
        eventName = $eventName
        processId = $PID
        scriptPath = $PSCommandPath
        cwd = if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'cwd') { [string]$payload.cwd } else { (Get-Location).Path }
        sessionId = if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'session_id') { [string]$payload.session_id } else { $null }
        turnId = if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'turn_id') { [string]$payload.turn_id } else { $null }
        toolName = if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'tool_name') { [string]$payload.tool_name } else { $null }
        transcriptPath = if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'transcript_path') { $payload.transcript_path } else { $null }
        parseError = $parseError
        raw = $inputJson
        payload = $payload
    }

    $jsonLine = $record | ConvertTo-Json -Depth 100 -Compress
    $prettyJson = $record | ConvertTo-Json -Depth 100
    $encoding = $utf8NoBom
    $datePrefix = Get-Date -Format 'yyyy-MM-dd'

    $allLog = Join-Path -Path $logRoot -ChildPath "$datePrefix-all.jsonl"
    $eventLog = Join-Path -Path $logRoot -ChildPath "$datePrefix-$eventName.jsonl"
    $lastEvent = Join-Path -Path $logRoot -ChildPath "last-$eventName.json"
    $lastAny = Join-Path -Path $logRoot -ChildPath 'last.json'

    Add-TextWithRetry -Path $allLog -Text ($jsonLine + [Environment]::NewLine) -Encoding $encoding
    Add-TextWithRetry -Path $eventLog -Text ($jsonLine + [Environment]::NewLine) -Encoding $encoding
    Set-TextWithRetry -Path $lastEvent -Text ($prettyJson + [Environment]::NewLine) -Encoding $encoding
    Set-TextWithRetry -Path $lastAny -Text ($prettyJson + [Environment]::NewLine) -Encoding $encoding
}
catch {
    $logRoot = Join-Path -Path $PSScriptRoot -ChildPath 'logs'
    New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
    $encoding = $utf8NoBom
    $errorRecord = [ordered]@{
        loggedAt = (Get-Date).ToString('o')
        eventName = 'LoggerError'
        error = $_.Exception.Message
        stack = $_.ScriptStackTrace
    } | ConvertTo-Json -Depth 20 -Compress
    Add-TextWithRetry -Path (Join-Path $logRoot 'logger-errors.jsonl') -Text ($errorRecord + [Environment]::NewLine) -Encoding $encoding
    exit 1
}
