# StandPulse.SensorHost

## Cel

`StandPulse.SensorHost.exe` jest lokalnym hostem sensora obecności StandPulse.

Program czyta dane z sensora przez port COM, stabilizuje status obecności i udostępnia go lokalnym aplikacjom C# przez Windows Named Pipe.

## Uruchomienie

Host uruchamia się komendą:

```powershell
.\StandPulse.SensorHost.exe run
```

Tryb diagnostyczny działa jak `run`, ale loguje surowe linie z sensora oraz decyzje interpretacji:

```powershell
.\StandPulse.SensorHost.exe debug
```

Bez argumentów program pokazuje pomoc i kończy działanie:

```powershell
.\StandPulse.SensorHost.exe
```

Pomoc można też wyświetlić komendami:

```powershell
.\StandPulse.SensorHost.exe help
.\StandPulse.SensorHost.exe -help
.\StandPulse.SensorHost.exe --help
```

Ścieżkę do tej dokumentacji zwraca komenda:

```powershell
.\StandPulse.SensorHost.exe doc
```

## Autostart

Do autostartu systemu należy użyć komendy:

```powershell
P:\StandPulse\bin\StandPulse.SensorHost\StandPulse.SensorHost.exe debug
```

Program działa w trybie ciągłym do czasu zamknięcia procesu.

Skrypt instalacyjny ustawia autostart dla bieżącego użytkownika:

```powershell
P:\StandPulse\scripts\install-sensorhost-user.ps1
```

Skrypt dodaje wpis `StandPulse.SensorHost` w:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

## PATH

Skrypt `install-sensorhost-user.ps1` dodaje do user `PATH` katalogi:

```text
P:\StandPulse\bin\StandPulse.SensorHost
P:\StandPulse\bin\StandPulse.SensorProbe
```

Po zmianie `PATH` trzeba otworzyć nowe okno terminala.

## Konfiguracja

Konfiguracja znajduje się w pliku:

```text
sensor-settings.json
```

Najważniejsze ustawienia:

1. `source.mode` - źródło próbek: `serial` dla realnego sensora albo `fake` dla testów.
2. `serial.portName` - port COM sensora, domyślnie `COM4`.
3. `serial.baudRate` - prędkość portu, domyślnie `115200`.
4. `presence.maxDistanceCm` - maksymalna odległość uznawana za obecność przy biurku.
5. `presence.stateChangeDelaySeconds` - minimalny czas utrzymania nowego stanu przed publikacją zmiany.
6. `presence.sampleTimeoutSeconds` - czas bez poprawnej próbki po którym status przechodzi w `sensorNotAvailable`.
7. `presence.maxConsecutiveSensorErrors` - liczba kolejnych błędów firmware po której status przechodzi w `error`.
8. `pipe.name` - nazwa Windows Named Pipe, domyślnie `StandPulse.Sensor`.

## Statusy

Host publikuje cztery statusy:

1. `presence` - użytkownik jest stabilnie obecny przy biurku.
2. `absence` - użytkownik jest stabilnie poza biurkiem.
3. `sensorNotAvailable` - sensor nie dostarcza aktualnych danych albo jest niedostępny.
4. `error` - host wykrył powtarzalny błąd firmware sensora.

Pojedyncza błędna linia z COM jest logowana, ale nie zmienia publicznego statusu.

## Komunikacja

Host udostępnia status przez Windows Named Pipe:

```text
StandPulse.Sensor
```

Komunikaty mają format JSON Lines, czyli jedna linia tekstu zawiera jeden komunikat JSON.

Pobranie aktualnego statusu:

```json
{"command":"get"}
```

Przykładowa odpowiedź:

```json
{"type":"status","status":"presence","lastSampleAt":"2026-05-07T18:40:00.123+02:00","stableSince":"2026-05-07T18:39:50.000+02:00"}
```

Subskrypcja zmian:

```json
{"command":"watch"}
```

W trybie `watch` host wysyła aktualny status po połączeniu oraz kolejne linie tylko po zmianie stabilnego statusu.

## Klient C#

Katalog `examples` zawiera pojedynczy plik klienta C#:

```text
examples\standpulse-sensor-client.cs
```

Plik można skopiować do innej aplikacji C# i używać metod:

1. `GetStatusAsync()` - pobiera aktualny status.
2. `WatchAsync()` - odbiera aktualny status i późniejsze zmiany.

Minimalne użycie:

```csharp
var client = new StandPulseSensorClient();
var current = await client.GetStatusAsync();

await foreach (var snapshot in client.WatchAsync())
{
  Console.WriteLine(snapshot.Status);
}
```

## Aplikacja testowa

Katalog:

```text
P:\StandPulse\bin\StandPulse.SensorProbe
```

zawiera prostą aplikację testową łączącą się z hostem przez named pipe.

Pobranie aktualnego statusu:

```powershell
StandPulse.SensorProbe.exe get
```

Subskrypcja trzech statusów:

```powershell
StandPulse.SensorProbe.exe watch --count 3
```

Wskazanie niestandardowej nazwy pipe:

```powershell
StandPulse.SensorProbe.exe get --pipe StandPulse.Sensor
```

## Test fake sensora

Tryb `fake` służy do sprawdzania działania hosta bez realnego sensora.

W repozytorium jest skrypt:

```powershell
P:\StandPulse\scripts\run-sensorhost-fake-scenarios.ps1
```

Skrypt uruchamia hosta na tymczasowej konfiguracji i sprawdza interpretację serii próbek zgodnie z ustawieniami `presence`.
