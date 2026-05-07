param(
    [string]$PortName = "COM4",
    [int]$BaudRate = 115200,
    [int]$Seconds = 15
)

$port = [System.IO.Ports.SerialPort]::new(
    $PortName,
    $BaudRate,
    [System.IO.Ports.Parity]::None,
    8,
    [System.IO.Ports.StopBits]::One
)

$port.ReadTimeout = 500
$port.DtrEnable = $false
$port.RtsEnable = $false

try {
    $port.Open()
    Start-Sleep -Milliseconds 1200

    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $port.ReadLine()
        }
        catch [System.TimeoutException] {
        }
    }
}
finally {
    if ($port.IsOpen) {
        $port.Close()
    }
}
