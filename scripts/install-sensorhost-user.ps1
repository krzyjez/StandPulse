Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$hostDir = Join-Path $repoRoot "bin\StandPulse.SensorHost"
$probeDir = Join-Path $repoRoot "bin\StandPulse.SensorProbe"
$hostExe = Join-Path $hostDir "StandPulse.SensorHost.exe"
$runKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$runValueName = "StandPulse.SensorHost"
$runValue = "`"$hostExe`" run"

if (-not (Test-Path -LiteralPath $hostExe)) {
    throw "Host exe not found: $hostExe. Run scripts\publish-sensorhost.ps1 first."
}

if (-not (Test-Path -LiteralPath $probeDir)) {
    throw "Probe publish directory not found: $probeDir. Run scripts\publish-sensorprobe.ps1 first."
}

function Add-UserPathEntry {
    param([string]$PathEntry)

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ([string]::IsNullOrWhiteSpace($currentPath)) {
        $entries = @()
    }
    else {
        $entries = $currentPath -split ";"
    }

    $exists = $false
    foreach ($entry in $entries) {
        $normalizedEntry = $entry.TrimEnd([char]92).TrimEnd([char]47)
        $normalizedPathEntry = $PathEntry.TrimEnd([char]92).TrimEnd([char]47)

        if ([string]::Equals($normalizedEntry, $normalizedPathEntry, [System.StringComparison]::OrdinalIgnoreCase)) {
            $exists = $true
            break
        }
    }

    if ($exists) {
        Write-Host "PATH already contains: $PathEntry"
        return
    }

    $updatedEntries = @($entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) + $PathEntry
    $updatedPath = $updatedEntries -join ";"
    [Environment]::SetEnvironmentVariable("Path", $updatedPath, "User")
    Write-Host "Added to user PATH: $PathEntry"
}

Add-UserPathEntry -PathEntry $hostDir
Add-UserPathEntry -PathEntry $probeDir

if (-not (Test-Path -LiteralPath $runKeyPath)) {
    New-Item -Path $runKeyPath -Force | Out-Null
}

Set-ItemProperty -Path $runKeyPath -Name $runValueName -Value $runValue

Write-Host "Autostart set:"
Write-Host "  $runValueName = $runValue"
Write-Host "Open a new terminal window to use updated PATH."
