# StandPulse.SensorHost

## Cel

`StandPulse.SensorHost.exe` jest lokalnym hostem sensora obecności StandPulse.

Program czyta dane z sensora przez port COM, stabilizuje status obecności i udostępnia go lokalnym aplikacjom C# przez Windows Named Pipe.

## Uruchomienie

Host uruchamia się komendą:

```powershell
.\StandPulse.SensorHost.exe run
```

Tryb `debug` działa tak samo jak `run`. Obie komendy zapisują zdarzenia operacyjne hosta do pliku logu, a nazwy `debug` używa autostart:

```powershell
.\StandPulse.SensorHost.exe debug
```

Tryb śledzenia działa jak `run`, ale dodatkowo zapisuje surowe linie i próbki sensora. Używaj go tylko do krótkiej diagnostyki:

```powershell
.\StandPulse.SensorHost.exe trace
```

Po starcie program wypisuje na konsoli ścieżkę logu. Jeden log operacyjny jest zapisywany w:

```text
%LOCALAPPDATA%\StandPulse\SensorHost\logs\sensorhost.log
```

Gdy log przekroczy 5 MB, program przenosi go do `sensorhost.log.1` i zaczyna nowy plik.

Program można uruchomić tylko raz dla danej nazwy pipe. Drugie uruchomienie kończy się komunikatem `StandPulse.SensorHost już działa dla pipe ...`. Ctrl+C zatrzymuje program i wyłącza aktywne efektory.

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
6. `presence.sampleTimeoutSeconds` - czas bez żadnej wiadomości z sensora, po którym status przechodzi w `sensorNotAvailable`.
7. `presence.maxConsecutiveSensorErrors` - liczba kolejnych błędów firmware po której status przechodzi w `error`.
8. `presence.maxStartupInvalidLines` - liczba niepoprawnych linii przed pierwszą poprawną wiadomością, po której status przechodzi w `error`.
9. `pipe.name` - nazwa Windows Named Pipe, domyślnie `StandPulse.Sensor`.
10. `effectors.directory` - katalog z plikami JSON opisującymi zewnętrzne efektory, domyślnie `effectors`.
11. `log.directory` - opcjonalny katalog logu; brak oznacza `%LOCALAPPDATA%\StandPulse\SensorHost\logs`.

Jeśli `sensor-settings.json` ma błąd składni (np. przecinek na końcu), program nie startuje i wypisuje przyczynę. Niepoprawna pojedyncza wartość jest zastępowana domyślną, a w logu pojawia się ostrzeżenie `Konfiguracja: ...`.

Efektory są definiowane w osobnych plikach:

```text
effectors\*.json
```

Przykład efektora Shelly Plug:

```json
{
  "name": "desk-plug",
  "type": "shellyPlug",
  "enabled": true,
  "connection": {
    "mode": "cloud",
    "serverUri": "https://shelly-258-eu.shelly.cloud",
    "deviceId": "d885ac1745e4",
    "authKeyEnvironmentVariable": "STANDPULSE_SHELLY_AUTH_KEY",
    "channel": 0,
    "commandTimeoutSeconds": 10,
    "retryCount": 2,
    "stateCheckIntervalSeconds": 30
  },
  "turnOnWhen": {
    "sensorStatus": "presence",
    "sessionStatus": "active"
  },
  "defaultState": "off"
}
```

Na razie obsługiwany typ efektora to `shellyPlug`. Reguła `turnOnWhen` opisuje warunki włączenia, a pozostałe stany prowadzą do `defaultState`, obecnie używanego jako bezpieczne `off`.

Efektor Shelly z przykładu działa według reguły:

```text
sensor presence + odblokowana sesja Windows -> Shelly ON
sensor absence/sensorNotAvailable/error albo zablokowana sesja Windows -> Shelly OFF
```

Zmiana `presence -> absence` korzysta z tej samej stabilizacji co publiczny status sensora, czyli z `presence.stateChangeDelaySeconds`. Blokada sesji Windows jest sprawdzana cyklicznie przez automatykę efektorów i zwykle prowadzi do wyłączenia przy następnym przebiegu pętli. Efektor Shelly okresowo odczytuje realny stan `switch:0.output` i koryguje gniazdko, jeżeli różni się ono od decyzji systemu. Przy kończeniu procesu host próbuje wysłać bezpieczne `OFF` do aktywnych efektorów. Nieudaną komendę Shelly host ponawia z rosnącym odstępem, najwyżej co 5 minut.

Klucz Cloud API nie powinien być zapisywany w pliku efektora. Dla domyślnej konfiguracji ustaw go w profilu użytkownika:

```powershell
[Environment]::SetEnvironmentVariable("STANDPULSE_SHELLY_AUTH_KEY", "<auth_key>", "User")
```

## Statusy

Host publikuje cztery statusy:

1. `presence` - użytkownik jest stabilnie obecny przy biurku.
2. `absence` - użytkownik jest stabilnie poza biurkiem.
3. `sensorNotAvailable` - sensor nie dostarcza aktualnych danych albo jest niedostępny.
4. `error` - host wykrył powtarzalny błąd firmware sensora.

Pojedyncza błędna linia z COM jest logowana, ale nie zmienia publicznego statusu.

Przy trwałym błędzie odczytu radaru status zostaje `error`. Status `sensorNotAvailable` oznacza, że port jest niedostępny albo sensor przestał cokolwiek wysyłać.

Przed pierwszą poprawną wiadomością host ignoruje krótki szum startowy ESP32 i ucięte linie. Jeśli jednak liczba takich niepoprawnych linii osiągnie `presence.maxStartupInvalidLines`, host publikuje status `error`.

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
{"type":"status","status":"presence","sensorStatus":"presence","sessionStatus":"active","workPresence":true,"lastSampleAt":"2026-05-07T18:40:00.123+02:00","stableSince":"2026-05-07T18:39:50.000+02:00","workPresenceSince":"2026-05-07T18:39:50.000+02:00"}
```

Pole `status` jest zgodnościowym aliasem `sensorStatus`. Nowe aplikacje powinny czytać:

1. `sensorStatus` - stabilny stan sensora: `presence`, `absence`, `sensorNotAvailable`, `error`.
2. `sessionStatus` - stan sesji Windows: `active`, `locked`, `unknown`.
3. `workPresence` - efektywna obecność robocza używana przez automatykę Shelly.
4. `workPresenceSince` - od kiedy obowiązuje bieżąca wartość `workPresence`.

Subskrypcja zmian:

```json
{"command":"watch"}
```

W trybie `watch` host wysyła aktualny status po połączeniu oraz kolejne linie po zmianie stabilnego statusu sensora albo statusu sesji Windows.

## Klient C#

Katalog `examples` zawiera pojedynczy plik klienta C#:

```text
examples\standpulse-sensor-client.cs
```

Plik można skopiować do innej aplikacji C# i używać metod:

1. `GetStatusAsync()` - pobiera aktualny status.
2. `WatchAsync()` - odbiera aktualny status i późniejsze zmiany.

Gdy host nie działa, połączenie kończy się `TimeoutException` po 5 s (czas można zmienić parametrem `connectTimeout` konstruktora). Odpowiedź hosta innego typu niż `status` kończy się `InvalidDataException`.

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
