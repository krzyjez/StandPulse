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

# Pusty katalog efektorów odcina fake hosta od prawdziwego gniazdka Shelly.
$emptyEffectorsDir = Join-Path $resolvedTempRoot "effectors"
New-Item -ItemType Directory -Path $emptyEffectorsDir | Out-Null

$pipeProbeSource = @'
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;

public static class StandPulseFakeScenarioPipeProbe
{
    /// <summary>Czyta kilka linii z komendy pipe (watch albo samples); przerywa, gdy kolejna linia nie przyjdzie w readTimeoutMs.</summary>
    public static string[] Read(string pipeName, string command, int maxLines, int connectTimeoutMs, int readTimeoutMs)
    {
        var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        pipe.Connect(connectTimeoutMs);

        var encoding = new UTF8Encoding(false);
        var request = encoding.GetBytes("{\"command\":\"" + command + "\"}\n");
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
            maxStartupInvalidLines = 3
        }
        pipe = [ordered]@{
            name = $PipeName
        }
        effectors = [ordered]@{
            directory = $emptyEffectorsDir
        }
        log = [ordered]@{
            directory = $resolvedTempRoot
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
        [string]$ScenarioName,
        [switch]$StayInLast
    )

    $expectedIndex = 0
    $statusIndex = 0

    foreach ($status in $Statuses) {
        if ($expectedIndex -lt $Expected.Count -and $status -eq $Expected[$expectedIndex]) {
            $expectedIndex++
            if ($expectedIndex -eq $Expected.Count) {
                break
            }
        }

        $statusIndex++
    }

    if ($expectedIndex -ne $Expected.Count) {
        throw "Scenario '$ScenarioName' failed. Expected sequence: $($Expected -join ', '), got: $($Statuses -join ', ')"
    }

    # StayInLast pilnuje, że po osiągnięciu ostatniego statusu host już go nie zmienia.
    if ($StayInLast) {
        $lastExpected = $Expected[$Expected.Count - 1]
        for ($i = $statusIndex + 1; $i -lt $Statuses.Count; $i++) {
            if ($Statuses[$i] -ne $lastExpected) {
                throw "Scenario '$ScenarioName' failed. Status left '$lastExpected'. Got: $($Statuses -join ', ')"
            }
        }
    }
}

function Invoke-FakeScenario {
    param(
        [string]$Name,
        [object[]]$Samples,
        [string[]]$ExpectedSequence,
        [string[]]$ForbiddenStatuses = @(),
        [int]$MaxLines = 4,
        [int]$ReadTimeoutMs = 5000,
        [switch]$StayInLast,
        [string]$Command = "watch",
        [int]$MinSampleLines = 0
    )

    $pipeName = "StandPulse.Sensor.FakeScenario." + [Guid]::NewGuid().ToString("N")
    $stdoutPath = Join-Path $resolvedTempRoot ($Name + ".out.log")
    $stderrPath = Join-Path $resolvedTempRoot ($Name + ".err.log")

    New-FakeSettingsJson -PipeName $pipeName -Samples $Samples |
        Set-Content -LiteralPath $settingsPath -Encoding utf8

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @($hostDllPath, "run") `
        -WorkingDirectory $repoRoot `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath

    try {
        Start-Sleep -Milliseconds 300

        $lines = [StandPulseFakeScenarioPipeProbe]::Read($pipeName, $Command, $MaxLines, 5000, $ReadTimeoutMs)
        if ($lines.Count -eq 0) {
            throw "Scenario '$Name' failed. No status lines received."
        }

        $messages = @($lines | ForEach-Object { $_ | ConvertFrom-Json })
        $statuses = @($messages | Where-Object { $_.type -eq "status" } | ForEach-Object { $_.status })
        $samples = @($messages | Where-Object { $_.type -eq "sample" })

        # Krótki ReadTimeoutMs sprawia, że przerwa między próbkami dłuższa niż limit kończy odczyt i obcina ich liczbę.
        if ($samples.Count -lt $MinSampleLines) {
            throw "Scenario '$Name' failed. Expected at least $MinSampleLines sample lines, got $($samples.Count)."
        }

        foreach ($sample in $samples) {
            if ($null -eq $sample.decision -or $null -eq $sample.at) {
                throw "Scenario '$Name' failed. Sample line without decision or at: $($sample | ConvertTo-Json -Compress)"
            }
        }

        Assert-ContainsStatusSequence -Statuses $statuses -Expected $ExpectedSequence -ScenarioName $Name -StayInLast:$StayInLast

        foreach ($forbiddenStatus in $ForbiddenStatuses) {
            if ($statuses -contains $forbiddenStatus) {
                throw "Scenario '$Name' failed. Forbidden status '$forbiddenStatus' was emitted. Got: $($statuses -join ', ')"
            }
        }

        [pscustomobject]@{
            Scenario = $Name
            Statuses = ($statuses -join " -> ") + $(if ($samples.Count -gt 0) { " ($($samples.Count) samples)" } else { "" })
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
    },
    @{
        Name = "silent-sensor-becomes-unavailable"
        ExpectedSequence = @("presence", "sensorNotAvailable")
        MaxLines = 3
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 14 },
            [ordered]@{ rawLine = "x"; event = $null; present = $false; targetStatus = "Silent"; distanceCm = $null; repeat = 40 }
        )
    },
    @{
        Name = "radar-errors-after-presence-stay-error"
        ExpectedSequence = @("presence", "error")
        StayInLast = $true
        MaxLines = 4
        ReadTimeoutMs = 3000
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 14 },
            [ordered]@{ event = "radar_read_error"; present = $false; targetStatus = "ErrorFrame"; distanceCm = $null; repeat = 40 }
        )
    },
    @{
        Name = "invalid-field-type-is-ignored"
        ExpectedSequence = @("presence")
        StayInLast = $true
        MaxLines = 3
        ReadTimeoutMs = 3000
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 5 },
            [ordered]@{ rawLine = '{"present":true,"distanceCm":64.5}'; event = $null; present = $false; targetStatus = "BadType"; distanceCm = $null; repeat = 1 },
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 5 },
            [ordered]@{ rawLine = '{"present":null,"distanceCm":64}'; event = $null; present = $false; targetStatus = "BadType"; distanceCm = $null; repeat = 1 }
        )
    },
    @{
        Name = "single-radar-errors-are-ignored"
        ExpectedSequence = @("presence")
        StayInLast = $true
        MaxLines = 3
        ReadTimeoutMs = 3000
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 4 },
            [ordered]@{ event = "radar_read_error"; present = $false; targetStatus = "ErrorFrame"; distanceCm = $null; repeat = 1 }
        )
    },
    @{
        Name = "error-recovers-to-presence"
        ExpectedSequence = @("error", "presence")
        MaxLines = 3
        Samples = @(
            [ordered]@{ event = "radar_read_error"; present = $false; targetStatus = "ErrorFrame"; distanceCm = $null; repeat = 3 },
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 14 }
        )
    },
    @{
        Name = "samples-stream-delivers-each-sample"
        Command = "samples"
        ExpectedSequence = @("presence")
        MinSampleLines = 12
        MaxLines = 20
        ReadTimeoutMs = 1000
        Samples = @(
            [ordered]@{ event = $null; present = $true; targetStatus = "StaticTarget"; distanceCm = 64; repeat = 30 }
        )
    },
    @{
        Name = "startup-garbage-becomes-error"
        ExpectedSequence = @("error")
        MaxLines = 2
        Samples = @(
            [ordered]@{ rawLine = "boot noise"; event = $null; present = $false; targetStatus = "StartupNoise"; distanceCm = $null; repeat = 3 }
        )
    }
)

try {
    dotnet build $projectPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

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

        $stayInLast = $scenario.ContainsKey("StayInLast") -and $scenario["StayInLast"]

        $command = "watch"
        if ($scenario.ContainsKey("Command")) {
            $command = $scenario["Command"]
        }

        $minSampleLines = 0
        if ($scenario.ContainsKey("MinSampleLines")) {
            $minSampleLines = $scenario["MinSampleLines"]
        }

        Invoke-FakeScenario `
            -Name $scenario["Name"] `
            -Samples $scenario["Samples"] `
            -ExpectedSequence $scenario["ExpectedSequence"] `
            -ForbiddenStatuses $forbiddenStatuses `
            -MaxLines $maxLines `
            -ReadTimeoutMs $readTimeoutMs `
            -StayInLast:$stayInLast `
            -Command $command `
            -MinSampleLines $minSampleLines
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
