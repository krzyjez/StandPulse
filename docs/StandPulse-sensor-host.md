# StandPulse SensorHost

## Rola

`StandPulse.SensorHost` jest lokalnym procesem odpowiedzialnym za produkcyjny odczyt sensora obecności i udostępnienie stabilnego statusu innym aplikacjom.

Host jest jedynym właścicielem portu COM w normalnym modelu pracy. Inne aplikacje nie czytają sensora bezpośrednio, tylko korzystają z Windows Named Pipe.

`StandPulse.ScannerViewer` jest wyjątkiem: pozostaje narzędziem diagnostycznym do rozpoznawania charakterystyki sensora i czyta port COM bezpośrednio.

## Przepływ danych

```text
mmWave sensor -> XIAO -> USB serial / COM -> StandPulse.SensorHost -> named pipe -> aplikacje klienckie
```

Firmware wysyła do komputera JSON Lines. Host czyta próbki, interpretuje je według konfiguracji, stabilizuje status sensora i publikuje go razem ze statusem sesji Windows oraz efektywną obecnością roboczą.

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

Tryb `debug` działa tak samo jak `run`. Obie komendy zapisują zdarzenia operacyjne hosta do pliku logu, a nazwy `debug` używa autostart:

```powershell
StandPulse.SensorHost.exe debug
```

Tryb śledzenia działa jak `run`, ale dodatkowo zapisuje surowe linie i próbki sensora. Używaj go tylko do krótkiej diagnostyki:

```powershell
StandPulse.SensorHost.exe trace
```

Po starcie program wypisuje na konsoli ścieżkę logu. Jeden log operacyjny jest zapisywany w:

```text
%LOCALAPPDATA%\StandPulse\SensorHost\logs\sensorhost.log
```

Katalog logu można zmienić ustawieniem `log.directory`. Gdy plik przekroczy 5 MB, host przenosi go do `sensorhost.log.1` (nadpisując poprzednią kopię) i zaczyna nowy.

Dla jednej nazwy pipe może działać tylko jeden host. Drugie uruchomienie kończy się komunikatem `StandPulse.SensorHost już działa dla pipe ...` i kodem wyjścia `3`.

Ctrl+C zatrzymuje host: kończy odczyt sensora i serwer pipe, a aktywne efektory dostają bezpieczne `OFF`.

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

Podgląd każdej próbki na żywo, z odległościami i energiami obu celów:

```powershell
StandPulse.SensorProbe.exe samples
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
6. `presence.sampleTimeoutSeconds` - czas bez żadnej wiadomości z firmware (próbki ani zdarzenia), po którym status przechodzi w `sensorNotAvailable`.
7. `presence.maxConsecutiveSensorErrors` - liczba kolejnych błędów firmware po której status przechodzi w `error`.
8. `presence.maxStartupInvalidLines` - liczba niepoprawnych linii przed pierwszą poprawną wiadomością, po której status przechodzi w `error`.
9. `pipe.name` - nazwa Windows Named Pipe.
10. `effectors.directory` - katalog z plikami JSON opisującymi zewnętrzne efektory, domyślnie `effectors`.
11. `log.directory` - opcjonalny katalog logu; brak oznacza `%LOCALAPPDATA%\StandPulse\SensorHost\logs`.

Jeśli `sensor-settings.json` nie da się odczytać albo ma błąd składni (np. przecinek na końcu), host nie startuje: wypisuje przyczynę na konsoli, próbuje dopisać ją do logu i kończy się kodem wyjścia `1`. Niepoprawna pojedyncza wartość (np. ujemny próg) jest zastępowana wartością domyślną z ostrzeżeniem `Konfiguracja: ...` w logu. Wartości domyślne odpowiadają dostarczonemu plikowi.

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

Zmiana `presence -> absence` korzysta z tej samej stabilizacji co publiczny status sensora, czyli z `presence.stateChangeDelaySeconds`. Blokada sesji Windows jest sprawdzana cyklicznie przez automatykę efektorów i zwykle prowadzi do wyłączenia przy następnym przebiegu pętli. Efektor Shelly okresowo odczytuje realny stan `switch:0.output` i koryguje gniazdko, jeżeli różni się ono od decyzji systemu. Przy kończeniu procesu host próbuje wysłać bezpieczne `OFF` do aktywnych efektorów.

Odpowiedź Shelly Cloud z `isok:false` (np. urządzenie offline) jest traktowana jak nieudana komenda, a treść `errors` trafia do logu. Po nieudanej komendzie host ponawia ją z rosnącym odstępem: 2 s, 4 s, 8 s i tak dalej, najwyżej co 5 minut. Gdy w tym czasie zmieni się oczekiwany stan gniazdka, próba następuje od razu.

Klucz Cloud API nie powinien być zapisywany w pliku efektora. Dla domyślnej konfiguracji ustaw go w profilu użytkownika:

```powershell
[Environment]::SetEnvironmentVariable("STANDPULSE_SHELLY_AUTH_KEY", "<auth_key>", "User")
```

## Interpretacja obecności

Podstawowa reguła obecności:

```text
presence = rawPresent && targetDistanceCm < maxDistanceCm
```

`rawPresent` wynika z firmware i statusu celu radaru. `targetDistanceCm` ogranicza obecność do strefy biurka. Host liczy ją w dwóch krokach:

1. Z jednej ramki bierze najmniejszą odległość spośród celów wskazanych przez status (`movingDistanceCm` dla `MovingTarget`, `staticDistanceCm` dla `StaticTarget`, oba dla `BothTargets`) i `distanceCm` (detection distance). Osoba siedzi bliżej sensora niż oparcie krzesła czy ściana, więc dalekie odbicia nie wypychają jej poza próg. Przy `NoTarget` odległości nie ma.
2. Wynik wygładza medianą z ostatnich 5 próbek (około 1 s), bo radar ma bramki co 75 cm i pojedyncze odczyty skaczą o kilkadziesiąt centymetrów. Próbka bez celu daje brak odległości od razu.

Pomiar 2026-10-21 przy biurku (100 próbek): odległości celu ruchomego i nieruchomego skakały od 77 do 300 cm, detection distance mieściło się w 75–162 cm, a reguła najmniejszej odległości ani razu nie przekroczyła 162 cm. W pustym pokoju detection distance pokazuje około 272 cm (ściana), ale radar zgłasza wtedy `NoTarget`.

Zmiana statusu jest publikowana dopiero wtedy, gdy kandydat utrzymuje się co najmniej przez `stateChangeDelaySeconds`.

## Status publiczny

Host publikuje cztery statusy:

1. `presence` - użytkownik jest stabilnie obecny przy biurku.
2. `absence` - użytkownik jest stabilnie poza biurkiem.
3. `sensorNotAvailable` - sensor nie dostarcza aktualnych danych albo jest niedostępny.
4. `error` - host wykrył powtarzalny błąd firmware sensora.

Pojedyncza błędna linia z portu COM, także poprawny JSON ze złym typem pola, jest logowana, ale nie zmienia publicznego statusu.

Zdarzenia `radar_read_error` dowodzą, że łącze z sensorem działa, więc przy trwałym błędzie radaru status zostaje `error` i nie przechodzi w `sensorNotAvailable`. Ten drugi status pojawia się dopiero wtedy, gdy przez `sampleTimeoutSeconds` z portu nie przyjdzie żadna wiadomość, albo gdy port jest niedostępny.

Przed pierwszą poprawną wiadomością host ignoruje krótki szum startowy ESP32 i ucięte linie. Jeśli jednak liczba takich niepoprawnych linii osiągnie `presence.maxStartupInvalidLines`, host publikuje status `error`.

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
{"type":"status","status":"presence","sensorStatus":"presence","sessionStatus":"active","workPresence":true,"lastSampleAt":"2026-05-07T18:40:00.123+02:00","stableSince":"2026-05-07T18:39:50.000+02:00","workPresenceSince":"2026-05-07T18:39:50.000+02:00"}
```

Pole `status` jest zgodnościowym aliasem `sensorStatus`. Nowe aplikacje powinny czytać:

1. `sensorStatus` - stabilny stan sensora: `presence`, `absence`, `sensorNotAvailable`, `error`.
2. `sessionStatus` - stan sesji Windows: `active`, `locked`, `unknown`.
3. `workPresence` - efektywna obecność robocza używana przez automatykę Shelly.
4. `workPresenceSince` - od kiedy obowiązuje bieżąca wartość `workPresence`.
5. `lastSampleAt` - czas ostatniej poprawnej próbki, aktualizowany przy każdej próbce.

Subskrypcja zmian:

```json
{"command":"watch"}
```

W trybie `watch` host wysyła aktualny snapshot po przyjęciu subskrypcji (zmiana z chwili subskrypcji może przyjść dodatkowo jako powtórzona linia), a potem wysyła kolejne linie po zmianie stabilnego statusu sensora albo statusu sesji Windows. Zmiana samego `lastSampleAt` nie tworzy zdarzenia.

Strumień próbek:

```json
{"command":"samples"}
```

Host wysyła najpierw aktualny snapshot `status` (taki sam jak w `get`), a potem każdą próbkę sensora od razu po odczycie z COM (około 5 na sekundę) oraz każdą zmianę stabilnego statusu jako linię `status`, tak jak w `watch`. Linie rozróżnia pole `type`. Przykładowa próbka:

```json
{"type":"sample","at":"2026-10-19T23:30:01.123+02:00","present":true,"targetStatus":"staticTarget","targetDistanceCm":64,"distanceCm":272,"movingDistanceCm":64,"movingEnergy":40,"staticDistanceCm":64,"staticEnergy":55,"maxDistanceCm":200,"decision":"presence"}
```

1. `at` - czas odczytu próbki przez host (ISO 8601 z offsetem).
2. `targetStatus` - `noTarget`, `movingTarget`, `staticTarget` albo `bothTargets`.
3. `targetDistanceCm` - wygładzona odległość osoby (opis wyżej w „Interpretacja obecności”); główna liczba do pokazania. `null`, gdy radar w tej próbce nie widzi celu.
4. `decision` - `presence` albo `absence` dla tej jednej próbki, przed stabilizacją `stateChangeDelaySeconds`. Gdy `targetDistanceCm` jest `null`, `decision` jest zawsze `absence`.
5. `distanceCm`, `movingDistanceCm`, `movingEnergy`, `staticDistanceCm`, `staticEnergy` - surowe pola radaru do diagnostyki; mogą być `null` przy starszym firmware.
6. `maxDistanceCm` - aktualny próg `presence.maxDistanceCm`.

Odłączenie sensora albo błąd widać od razu jako linię `status` z `sensorStatus` równym `sensorNotAvailable` albo `error`; próbki wtedy przestają przychodzić. Zerwane połączenie z pipe oznacza, że host nie działa. Kolejka próbek na klienta ma limit 256 linii i przy zapełnieniu odrzuca najstarsze, więc wolny klient nie blokuje hosta.

## Fake sensor

Tryb `fake` służy do sprawdzania logiki hosta bez realnego sensora.

Fake źródło generuje serie próbek opisane w `sensor-settings.json`. Dzięki temu da się sprawdzić stabilizację, progi dystansu, timeouty i przejście w `error` bez użycia COM.

Skrypt scenariuszy uruchamia hosta z osobnym pipe, logiem w katalogu tymczasowym i pustym katalogiem efektorów, więc nie steruje prawdziwym Shelly i może działać obok hosta z autostartu:

```powershell
.\scripts\run-sensorhost-fake-scenarios.ps1
```

Aktualnie sprawdzane scenariusze:

1. stabilna zmiana `presence -> absence`,
2. krótka nieobecność ignorowana przez stabilizację,
3. wykryty cel poza `maxDistanceCm` jako `absence`,
4. powtarzalne `radar_read_error` jako `error`,
5. milczący sensor po obecności jako `sensorNotAvailable`,
6. trwałe `radar_read_error` po obecności zostaje `error`,
7. linie ze złym typem pola nie zmieniają `presence`,
8. pojedyncze `radar_read_error` między próbkami nie dają `error`,
9. powrót z `error` do `presence`,
10. strumień `samples` dostarcza kolejne próbki z odstępem poniżej 1 s,
11. szum startowy jako `error`.

## Publikacja

Publikacja programu do rootowego katalogu `bin`:

```powershell
.\scripts\publish-sensorhost.ps1
```

Skrypty publikacji budują program w katalogu tymczasowym i podmieniają pliki w `bin` dopiero po udanym buildzie. Odmawiają pracy, gdy program działa z katalogu docelowego, więc host z autostartu trzeba przed publikacją zatrzymać.

Publikacja aplikacji testowej:

```powershell
.\scripts\publish-sensorprobe.ps1
```

Instalacja user-level dodająca hosta do autostartu oraz katalogi `bin` do user `PATH`:

```powershell
.\scripts\install-sensorhost-user.ps1
```

Autostart używa trybu `debug` (takiego samego jak `run`), więc po zalogowaniu host zapisuje zdarzenia operacyjne, zmiany statusu i komendy Shelly do jednego logu. Surowe próbki sensora zapisuje tylko ręcznie uruchomiony tryb `trace`.

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
