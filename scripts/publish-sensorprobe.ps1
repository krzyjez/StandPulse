param(
    [string]$Runtime = "win-x64",
    [ValidateSet("true", "false")]
    [string]$SelfContained = "false"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\StandPulse.SensorProbe\StandPulse.SensorProbe.csproj"
$publishDir = Join-Path $repoRoot "bin\StandPulse.SensorProbe"

$expectedPublishRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "bin"))
$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)

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

Write-Host "Published StandPulse.SensorProbe to: $publishDir"
Write-Host "Run with:"
Write-Host "  $publishDir\StandPulse.SensorProbe.exe get"
