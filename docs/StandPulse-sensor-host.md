# StandPulse SensorHost

## Rola

`StandPulse.SensorHost` jest lokalnym procesem odpowiedzialnym za produkcyjny odczyt sensora obecności i udostępnienie stabilnego statusu innym aplikacjom.

Host jest jedynym właścicielem portu COM w normalnym modelu pracy. Inne aplikacje nie czytają sensora bezpośrednio, tylko korzystają z Windows Named Pipe.

`StandPulse.ScannerViewer` jest wyjątkiem: pozostaje narzędziem diagnostycznym do rozpoznawania charakterystyki sensora i czyta port COM bezpośrednio.

## Przepływ danych

```text
mmWave sensor -> XIAO -> USB serial / COM -> StandPulse.SensorHost -> named pipe -> aplikacje klienckie
```

Firmware wysyła do komputera JSON Lines. Host czyta próbki, interpretuje je według konfiguracji, stabilizuje status i publikuje tylko domenowy stan obecności.

## Projekty i pliki

Kod hosta:

```text
src/StandPulse.SensorHost
```

Opublikowany program:

```text
bin/StandPulse.SensorHost
```

Testowy klient named pipe:

```text
bin/StandPulse.SensorProbe
```

Dokumentacja użytkowa opublikowanego programu:

```text
bin/StandPulse.SensorHost/StandPulse.SensorHost.md
```

Kopiowalny klient C# dla innych aplikacji:

```text
bin/StandPulse.SensorHost/examples/standpulse-sensor-client.cs
```

## CLI

Host uruchamia się komendą:

```powershell
StandPulse.SensorHost.exe run
```

Bez argumentów oraz dla `help`, `-help` i `--help` program pokazuje pomoc.

Komenda `doc` wypisuje ścieżkę do pliku dokumentacji Markdown leżącego obok programu:

```powershell
StandPulse.SensorHost.exe doc
```

## SensorProbe

`StandPulse.SensorProbe` jest prostą aplikacją testową potwierdzającą, że `StandPulse.SensorHost` odpowiada przez Windows Named Pipe.

Pobranie aktualnego statusu:

```powershell
StandPulse.SensorProbe.exe get
```

Subskrypcja ograniczona do kilku komunikatów:

```powershell
StandPulse.SensorProbe.exe watch --count 3
```

## Konfiguracja

Host używa pliku:

```text
sensor-settings.json
```

Najważniejsze ustawienia:

1. `source.mode` - `serial` dla realnego sensora albo `fake` dla testów.
2. `serial.portName` - port COM, domyślnie `COM4`.
3. `serial.baudRate` - prędkość portu, domyślnie `115200`.
4. `presence.maxDistanceCm` - maksymalna odległość uznawana za obecność przy biurku.
5. `presence.stateChangeDelaySeconds` - czas utrzymania kandydata przed zmianą stabilnego statusu.
6. `presence.sampleTimeoutSeconds` - czas bez poprawnej próbki po którym status przechodzi w `sensorNotAvailable`.
7. `presence.maxConsecutiveSensorErrors` - liczba kolejnych błędów firmware po której status przechodzi w `error`.
8. `pipe.name` - nazwa Windows Named Pipe.

## Interpretacja obecności

Podstawowa reguła obecności:

```text
presence = rawPresent && distanceCm < maxDistanceCm
```

`rawPresent` wynika z firmware i statusu celu radaru. `distanceCm` ogranicza obecność do strefy biurka.

Zmiana statusu jest publikowana dopiero wtedy, gdy kandydat utrzymuje się co najmniej przez `stateChangeDelaySeconds`.

## Status publiczny

Host publikuje cztery statusy:

1. `presence` - użytkownik jest stabilnie obecny przy biurku.
2. `absence` - użytkownik jest stabilnie poza biurkiem.
3. `sensorNotAvailable` - sensor nie dostarcza aktualnych danych albo jest niedostępny.
4. `error` - host wykrył powtarzalny błąd firmware sensora.

Pojedyncza błędna linia z portu COM jest logowana, ale nie zmienia publicznego statusu.

## Named Pipe

Domyślna nazwa pipe:

```text
StandPulse.Sensor
```

W Windows pełna ścieżka techniczna to:

```text
\\.\pipe\StandPulse.Sensor
```

Komunikacja używa JSON Lines.

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

W trybie `watch` host wysyła aktualny snapshot po przyjęciu subskrypcji, a potem wysyła kolejne linie tylko po zmianie stabilnego statusu. Zmiana samego `lastSampleAt` nie tworzy zdarzenia.

## Fake sensor

Tryb `fake` służy do sprawdzania logiki hosta bez realnego sensora.

Fake źródło generuje serie próbek opisane w `sensor-settings.json`. Dzięki temu da się sprawdzić stabilizację, progi dystansu, timeouty i przejście w `error` bez użycia COM.

Skrypt scenariuszy:

```powershell
.\scripts\run-sensorhost-fake-scenarios.ps1
```

Aktualnie sprawdzane scenariusze:

1. stabilna zmiana `presence -> absence`,
2. krótka nieobecność ignorowana przez stabilizację,
3. wykryty cel poza `maxDistanceCm` jako `absence`,
4. powtarzalne `radar_read_error` jako `error`.

## Publikacja

Publikacja programu do rootowego katalogu `bin`:

```powershell
.\scripts\publish-sensorhost.ps1
```

Publikacja aplikacji testowej:

```powershell
.\scripts\publish-sensorprobe.ps1
```

Instalacja user-level dodająca hosta do autostartu oraz katalogi `bin` do user `PATH`:

```powershell
.\scripts\install-sensorhost-user.ps1
```

Skrypt publikuje hosta do:

```text
bin/StandPulse.SensorHost
```

Rootowy katalog `bin` jest częścią repozytorium. Buildowe katalogi `src/**/bin` są ignorowane.

## Dokumentacja użytkowa

Dokumentacja dla osoby uruchamiającej gotowy program znajduje się obok exe:

```text
bin/StandPulse.SensorHost/StandPulse.SensorHost.md
```

Ten dokument w `docs` opisuje stan techniczny dla osób rozwijających, zmieniających albo testujących hosta.
