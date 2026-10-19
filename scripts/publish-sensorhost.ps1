param(
    [string]$Runtime = "win-x64",
    [ValidateSet("true", "false")]
    [string]$SelfContained = "false"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\StandPulse.SensorHost\StandPulse.SensorHost.csproj"
$publishDir = Join-Path $repoRoot "bin\StandPulse.SensorHost"

$resolvedRepoRoot = [System.IO.Path]::GetFullPath($repoRoot)
$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)
$expectedPublishRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "bin"))

if (-not $resolvedPublishDir.StartsWith($expectedPublishRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected publish path: $resolvedPublishDir"
}

# Publikacja nie może podmieniać plików działającego programu - część usunięć by się powiodła, a reszta nie.
$runningProcesses = @(
    Get-Process -Name "StandPulse.SensorHost" -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and $_.Path.StartsWith($resolvedPublishDir, [System.StringComparison]::OrdinalIgnoreCase) }
)
if ($runningProcesses.Count -gt 0) {
    throw "StandPulse.SensorHost is running from $resolvedPublishDir (PID: $($runningProcesses.Id -join ', ')). Stop it before publishing."
}

# Build trafia najpierw do katalogu tymczasowego, żeby błąd kompilacji nie rozebrał paczki w bin.
$stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("standpulse.sensorhost-publish-" + [Guid]::NewGuid().ToString("N"))

try {
    dotnet publish `
        $projectPath `
        -c Release `
        -r $Runtime `
        --self-contained $SelfContained `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $stagingDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    if (Test-Path -LiteralPath $resolvedPublishDir) {
        Get-ChildItem -LiteralPath $resolvedPublishDir -File |
            Where-Object { $_.Extension -in ".dll", ".exe", ".json", ".pdb" } |
            Remove-Item -Force
    }
    else {
        New-Item -ItemType Directory -Path $resolvedPublishDir | Out-Null
    }

    Copy-Item -Path (Join-Path $stagingDir "*") -Destination $resolvedPublishDir -Recurse -Force
}
finally {
    if (Test-Path -LiteralPath $stagingDir) {
        Remove-Item -LiteralPath $stagingDir -Recurse -Force
    }
}

Write-Host "Published StandPulse.SensorHost to: $publishDir"
Write-Host "Run with:"
Write-Host "  $publishDir\StandPulse.SensorHost.exe run"
Write-Host "Debug with:"
Write-Host "  $publishDir\StandPulse.SensorHost.exe debug"
Write-Host "Trace with:"
Write-Host "  $publishDir\StandPulse.SensorHost.exe trace"
