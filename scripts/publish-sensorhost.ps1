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

if (Test-Path -LiteralPath $resolvedPublishDir) {
    Get-ChildItem -LiteralPath $resolvedPublishDir -File |
        Where-Object { $_.Extension -in ".dll", ".exe", ".json", ".pdb" } |
        Remove-Item -Force
}
else {
    New-Item -ItemType Directory -Path $resolvedPublishDir | Out-Null
}

dotnet publish `
    $projectPath `
    -c Release `
    -r $Runtime `
    --self-contained $SelfContained `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

Write-Host "Published StandPulse.SensorHost to: $publishDir"
Write-Host "Run with:"
Write-Host "  $publishDir\StandPulse.SensorHost.exe run"
