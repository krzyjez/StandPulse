param(
    [switch]$KeepLogs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\StandPulse.SensorHost\StandPulse.SensorHost.csproj"
$outputDir = Join-Path $repoRoot "src\StandPulse.SensorHost\bin\Debug\net9.0-windows"
$hostDllPath = Join-Path $outputDir "StandPulse.SensorHost.dll"
$settingsPath = Join-Path $outputDir "sensor-settings.json"
$backupPath = Join-Path $outputDir "sensor-settings.json.fake-test.bak"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("standpulse-sensorhost-fake-tests-" + [Guid]::NewGuid().ToString("N"))

$resolvedTempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$resolvedTempRoot = [System.IO.Path]::GetFullPath($tempRoot)
if (-not $resolvedTempRoot.StartsWith($resolvedTempBase, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected temp path: $resolvedTempRoot"
}

New-Item -ItemType Directory -Path $resolvedTempRoot | Out-Null

$pipeProbeSource = @'
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;

public static class StandPulseFakeScenarioPipeProbe
{
    /// <summary>Czyta kilka linii statusu z komendy watch.</summary>
    public static string[] Watch(string pipeName, int maxLines, int connectTimeoutMs, int readTimeoutMs)
    {
        var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        pipe.Connect(connectTimeoutMs);

        var encoding = new UTF8Encoding(false);
        var request = encoding.GetBytes("{\"command\":\"watch\"}\n");
        pipe.Write(request, 0, request.Length);
        pipe.Flush();

        var reader = new StreamReader(pipe, encoding, false, 1024, true);
        var lines = new List<string>();

        for (var i = 0; i < maxLines; i++)
        {
            var task = reader.ReadLineAsync();
            if (!task.Wait(readTimeoutMs))
                break;

            var line = task.Result;
            if (line == null)
                break;

            lines.Add(line);
        }

        reader.Dispose();
        pipe.Dispose();

        return lines.ToArray();
    }
}
'@

Add-Type -TypeDefinition $pipeProbeSource -Language CSharp

function New-FakeSettingsJson {
    param(
        [string]$PipeName,
        [object[]]$Samples
    )

    $settings = [ordered]@{
        source = [ordered]@{
            mode = "fake"
        }
        serial = [ordered]@{
            portName = "COM4"
            baudRate = 115200
        }
        presence = [ordered]@{
            maxDistanceCm = 120
            stateChangeDelaySeconds = 1
            sampleTimeoutSeconds = 2
            maxConsecutiveSensorErrors = 3
        }
        pipe = [ordered]@{
            name = $PipeName
        }
        fake = [ordered]@{
            intervalMilliseconds = 100
            samples = $Samples
        }
    }

    return $settings | ConvertTo-Json -Depth 8
}

function Assert-ContainsStatusSequence {
    param(
        [string[]]$Statuses,
        [string[]]$Expected,
        [string]$ScenarioName
    )

    $expectedIndex = 0

    foreach ($status in $Statuses) {
        if ($expectedIndex -lt $Expected.Count -and $status -eq $Expected[$expectedIndex]) {
            $expectedIndex++
        }
    }

    if ($expectedIndex -ne $Expected.Count) {
        throw "Scenario '$ScenarioName' failed. Expected sequence: $($Expected -join ', '), got: $($Statuses -join ', ')"
    }
}

function Invoke-FakeScenario {
    param(
        [string]$Name,
        [object[]]$Samples,
        [string[]]$ExpectedSequence,
        [string[]]$ForbiddenStatuses = @(),
        [int]$MaxLines = 4,
        [int]$ReadTimeoutMs = 5000
    )

    $pipeName = "StandPulse.Sensor.FakeScenario." + [Guid]::NewGuid().ToString("N")
    $stdoutPath = Join-Path $resolvedTempRoot ($Name + ".out.log")
    $stderrPath = Join-Path $resolvedTempRoot ($Name + ".err.log")

    New-FakeSettingsJson -PipeName $pipeName -Samples $Samples |
        Set-Content -LiteralPath $settingsPath -Encoding utf8

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @($hostDllPath) `
        -WorkingDirectory $repoRoot `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath

    try {
        Start-Sleep -Milliseconds 300

        $lines = [StandPulseFakeScenarioPipeProbe]::Watch($pipeName, $MaxLines, 5000, $ReadTimeoutMs)
        if ($lines.Count -eq 0) {
            throw "Scenario '$Name' failed. No status lines received."
        }

        $statuses = @(
            foreach ($line in $lines) {
                ($line | ConvertFrom-Json).status
            }
        )

        Assert-ContainsStatusSequence -Statuses $statuses -Expected $ExpectedSequence -ScenarioName $Name

        foreach ($forbiddenStatus in $ForbiddenStatuses) {
            if ($statuses -contains $forbiddenStatus) {
                throw "Scenario '$Name' failed. Forbidden status '$forbiddenStatus' was emitted. Got: $($statuses -join ', ')"
            }
        }

        [pscustomobject]@{
            Scenario = $Name
            Statuses = ($statuses -join " -> ")
            Result = "OK"
        }
    }
    finally {
        if (!$process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }
}

$scenarios = @(
    @{
        Name = "stable-presence-then-absence"
        ExpectedSequence = @("presence", "absence")
        MaxLines = 3
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 14 },
            [ordered]@{ event = $null; present = $false; targetStatus = "NoTarget"; distanceCm = $null; repeat = 14 }
        )
    },
    @{
        Name = "short-absence-is-ignored"
        ExpectedSequence = @("presence")
        ForbiddenStatuses = @("absence")
        MaxLines = 3
        ReadTimeoutMs = 4500
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 14 },
            [ordered]@{ event = $null; present = $false; targetStatus = "NoTarget"; distanceCm = $null; repeat = 5 },
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 14 }
        )
    },
    @{
        Name = "distance-above-threshold-is-absence"
        ExpectedSequence = @("absence")
        MaxLines = 2
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 200; repeat = 14 }
        )
    },
    @{
        Name = "repeated-radar-errors-become-error"
        ExpectedSequence = @("error")
        MaxLines = 2
        Samples = @(
            [ordered]@{ event = "radar_read_error"; present = $false; targetStatus = "ErrorFrame"; distanceCm = $null; repeat = 3 }
        )
    }
)

try {
    dotnet build $projectPath | Out-Host

    if (Test-Path -LiteralPath $backupPath) {
        Remove-Item -LiteralPath $backupPath -Force
    }

    Copy-Item -LiteralPath $settingsPath -Destination $backupPath -Force

    $results = foreach ($scenario in $scenarios) {
        $forbiddenStatuses = @()
        if ($scenario.ContainsKey("ForbiddenStatuses")) {
            $forbiddenStatuses = $scenario["ForbiddenStatuses"]
        }

        $maxLines = 4
        if ($scenario.ContainsKey("MaxLines")) {
            $maxLines = $scenario["MaxLines"]
        }

        $readTimeoutMs = 5000
        if ($scenario.ContainsKey("ReadTimeoutMs")) {
            $readTimeoutMs = $scenario["ReadTimeoutMs"]
        }

        Invoke-FakeScenario `
            -Name $scenario["Name"] `
            -Samples $scenario["Samples"] `
            -ExpectedSequence $scenario["ExpectedSequence"] `
            -ForbiddenStatuses $forbiddenStatuses `
            -MaxLines $maxLines `
            -ReadTimeoutMs $readTimeoutMs
    }

    $results | Format-Table -AutoSize
}
finally {
    if (Test-Path -LiteralPath $backupPath) {
        Copy-Item -LiteralPath $backupPath -Destination $settingsPath -Force
        Remove-Item -LiteralPath $backupPath -Force
    }

    if (-not $KeepLogs -and (Test-Path -LiteralPath $resolvedTempRoot)) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
    elseif ($KeepLogs) {
        Write-Host "Logs kept in: $resolvedTempRoot"
    }
}
