# StandPulse - komunikacja z sensorem

## Decyzja

Program czytający sensor będzie udostępniał stan innym lokalnym programom C# przez **Windows Named Pipe**.

Nie używamy HTTP ani gRPC na tym etapie.

Powód:

1. wszystkie programy klienckie mają być w C#,
2. komunikacja jest tylko lokalna na Windows,
3. `COM4` może być otwarty tylko przez jeden proces naraz,
4. named pipe jest prosty, lekki i wystarczający,
5. gRPC byłoby tu zbyt ciężkie względem potrzeb.

## Docelowy model

Tylko jeden proces dotyka portu szeregowego sensora:

```text
XIAO / COM4
  -> StandPulse.SensorHost
  -> named pipe: StandPulse.Sensor
  -> inne programy C#
```

`StandPulse.SensorHost`:

1. otwiera `COM4`,
2. czyta JSON Lines z firmware XIAO,
3. interpretuje próbki,
4. stosuje reguły z `sensor-settings.json`,
5. stabilizuje stan przez zadany czas,
6. wystawia aktualny stan przez named pipe.

Inne programy nie czytają `COM4` bezpośrednio.

## Proponowane projekty

```text
src/StandPulse.SensorHost
```

Proces usługowy / host sensora. Czyta `COM4` i wystawia named pipe.

```text
src/StandPulse.SensorClient
```

Mała biblioteka C# dla innych programów. Ukrywa szczegóły named pipe.

```text
src/StandPulse.ScannerViewer
```

Okienko diagnostyczne. Docelowo powinno czytać stan z `StandPulse.SensorHost`, a nie bezpośrednio z `COM4`.

## Nazwa pipe

Robocza nazwa pipe:

```text
StandPulse.Sensor
```

W Windows pełna ścieżka techniczna to:

```text
\\.\pipe\StandPulse.Sensor
```

W C# zwykle używamy samej nazwy:

```csharp
new NamedPipeClientStream(".", "StandPulse.Sensor", PipeDirection.InOut)
```

## Format komunikatów

Komunikaty będą przesyłane jako **JSON Lines**:

1. jeden komunikat = jedna linia JSON,
2. tekst UTF-8,
3. koniec komunikatu = znak nowej linii.

To jest proste do obsługi przez:

```csharp
StreamReader.ReadLineAsync()
StreamWriter.WriteLineAsync()
System.Text.Json
```

## Komenda: pobranie aktualnego stanu

Klient wysyła:

```json
{"command":"get"}
```

Host odpowiada:

```json
{
  "type": "status",
  "presentAtDesk": true,
  "rawPresent": true,
  "distanceCm": 64,
  "targetStatus": "StaticTarget",
  "lastSampleAt": "2026-05-07T18:40:00.123+02:00",
  "stableSince": "2026-05-07T18:39:50.000+02:00"
}
```

Znaczenie pól:

| Pole | Znaczenie |
|---|---|
| `presentAtDesk` | Przefiltrowany stan używany przez StandPulse. |
| `rawPresent` | Surowa obecność z radaru, bez reguł dystansu i stabilizacji. |
| `distanceCm` | Ostatnia odległość z radaru w centymetrach. |
| `targetStatus` | Surowy status celu z radaru. |
| `lastSampleAt` | Czas ostatniej poprawnej próbki. |
| `stableSince` | Od kiedy aktualny stan jest stabilny. |

## Komenda: subskrypcja zmian

Klient wysyła:

```json
{"command":"watch"}
```

Host utrzymuje połączenie i wysyła zdarzenia tylko wtedy, gdy stabilny stan się zmieni:

```json
{"type":"stateChanged","presentAtDesk":false,"distanceCm":145,"targetStatus":"StaticTarget","changedAt":"2026-05-07T18:45:12.000+02:00"}
{"type":"stateChanged","presentAtDesk":true,"distanceCm":72,"targetStatus":"BothTargets","changedAt":"2026-05-07T18:48:21.000+02:00"}
```

Nie wysyłamy każdej próbki z radaru do każdego klienta.
Do większości zastosowań wystarczą:

1. aktualny status na żądanie,
2. zdarzenie zmiany stabilnego stanu.

## Konfiguracja sensora

Reguły interpretacji powinny zostać w `sensor-settings.json`, np.:

```json
{
  "serial": {
    "portName": "COM4",
    "baudRate": 115200
  },
  "presence": {
    "maxDistanceCm": 120,
    "stateChangeDelaySeconds": 10
  }
}
```

Znaczenie:

```text
presentAtDesk = rawPresent && distanceCm < maxDistanceCm
```

Zmiana stanu jest akceptowana dopiero wtedy, gdy kandydat utrzymuje się przez `stateChangeDelaySeconds`.

## Dlaczego nie COM4 w każdym programie

Port szeregowy jest zasobem wyłącznym.
Jeśli jeden program otworzy `COM4`, drugi zwykle nie może go już otworzyć.

Dlatego `COM4` powinien mieć jednego właściciela:

```text
StandPulse.SensorHost
```

Reszta programów komunikuje się z hostem przez named pipe.

## Minimalny klient C# - szkic

```csharp
using System.IO.Pipes;
using System.Text;

await using var pipe = new NamedPipeClientStream(
    ".",
    "StandPulse.Sensor",
    PipeDirection.InOut,
    PipeOptions.Asynchronous);

await pipe.ConnectAsync();

using var reader = new StreamReader(pipe, Encoding.UTF8);
await using var writer = new StreamWriter(pipe, Encoding.UTF8)
{
    AutoFlush = true
};

await writer.WriteLineAsync("{\"command\":\"get\"}");
var response = await reader.ReadLineAsync();
```

Docelowo ten kod powinien trafić do `StandPulse.SensorClient`, żeby inne aplikacje nie musiały znać szczegółów protokołu.

## Następny krok

Najpierw można zostawić `ScannerViewer` czytający `COM4`, bo działa jako narzędzie diagnostyczne.

Następny etap architektoniczny:

1. wydzielić logikę czytania sensora do `StandPulse.SensorHost`,
2. przenieść stabilizację stanu do hosta,
3. dodać named pipe `StandPulse.Sensor`,
4. przepiąć `ScannerViewer` na klienta pipe.
