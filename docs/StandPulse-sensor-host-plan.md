# StandPulse SensorHost

## Cel

`StandPulse.SensorHost` jest lokalnym procesem odpowiedzialnym za udostępnianie stabilnego stanu obecności przy biurku innym aplikacjom StandPulse.

Proces jest jedynym właścicielem połączenia z portem szeregowym sensora. Pozostałe aplikacje korzystają z Windows Named Pipe i nie otwierają portu COM bezpośrednio.

## Nazwa aplikacji

Nazwa aplikacji: `StandPulse.SensorHost`.

Uzasadnienie:

1. nazwa jest zgodna z nazwami projektów .NET,
2. jasno wskazuje, że proces jest hostem sensora,
3. nie sugeruje, że aplikacja odpowiada za logikę czasu pracy, alarmy albo UI,
4. pasuje do prostego pliku klienckiego kopiowanego do innych aplikacji.

## Odpowiedzialność

`StandPulse.SensorHost` odpowiada za:

1. otwarcie portu COM sensora,
2. odczyt JSON Lines wysyłanych przez firmware,
3. interpretację surowych próbek sensora,
4. zastosowanie reguł obecności przy biurku,
5. stabilizację zmian stanu,
6. utrzymywanie aktualnego snapshotu stanu,
7. udostępnianie stanu przez Windows Named Pipe.

`StandPulse.SensorHost` nie odpowiada za:

1. naliczanie czasu pracy,
2. resetowanie cyklu pracy,
3. pokazywanie alarmów,
4. wyświetlanie UI użytkownika,
5. decyzje domenowe aplikacji monitorującej czas pracy.

## Publiczny status

Publiczny kontrakt udostępnia stan domenowy, a nie surowe dane radaru.

Dozwolone wartości pola `status`:

1. `presence` - użytkownik jest stabilnie obecny przy biurku,
2. `absence` - użytkownik jest stabilnie poza biurkiem,
3. `sensorNotAvailable` - host nie ma aktualnych danych z sensora albo sensor jest niedostępny,
4. `error` - host wykrył błąd, którego nie da się opisać jako zwykły brak sensora.

## Snapshot stanu

Odpowiedź na komendę `get` zawiera aktualny snapshot:

```json
{
  "type": "status",
  "status": "presence",
  "lastSampleAt": "2026-05-07T18:40:00.123+02:00",
  "stableSince": "2026-05-07T18:39:50.000+02:00"
}
```

Znaczenie pól:

| Pole | Znaczenie |
|---|---|
| `type` | Typ komunikatu. Dla snapshotu ma wartość `status`. |
| `status` | Stabilny stan domenowy udostępniany aplikacjom StandPulse. |
| `lastSampleAt` | Czas ostatniej poprawnie przetworzonej próbki sensora. |
| `stableSince` | Czas, od którego aktualny `status` jest stabilny. |

## Tryb get

Klient wysyła jedną komendę:

```json
{"command":"get"}
```

Host odpowiada jednym snapshotem `status`. Po tej odpowiedzi połączenie może zostać zamknięte.

Tryb `get` służy do pobrania aktualnego stanu przy starcie aplikacji albo po ponownym połączeniu.

## Tryb watch

Klient wysyła komendę:

```json
{"command":"watch"}
```

Host utrzymuje połączenie i wysyła komunikat `status`:

1. od razu po przyjęciu subskrypcji,
2. po każdej zmianie stabilnego statusu.

Host nie wysyła każdej próbki sensora do klientów. Zmiany `lastSampleAt` bez zmiany pola `status` nie tworzą zdarzenia w trybie `watch`.

## Częstotliwość odczytu i publikacji

Firmware może publikować surowe próbki kilka razy na sekundę. `StandPulse.SensorHost` czyta te próbki stale, ponieważ są potrzebne do wykrywania obecności i stabilizacji.

Publikacja do klientów jest rzadsza i zależy od zmiany stabilnego statusu. Aplikacja monitorująca czas pracy nie polluje sensora co sekundę. Aktualizuje własny licznik lokalnym timerem, a zmianę obecności otrzymuje przez `watch`.

## Użycie w innych aplikacjach

Inne aplikacje korzystają z gotowego pliku klienta `bin/StandPulse.SensorHost/examples/standpulse-sensor-client.cs`.

Przepływ:

```text
sensor -> StandPulse.SensorHost -> named pipe -> aplikacja kliencka
```

Aplikacja kliencka:

1. kopiuje plik `standpulse-sensor-client.cs` do własnego projektu C#,
2. uruchamia `StandPulse.SensorHost` jako źródło statusu sensora,
3. przy starcie wykonuje `GetStatusAsync`,
4. następnie używa `WatchAsync` do odbierania aktualnego statusu i późniejszych zmian,
5. nie otwiera portu COM bezpośrednio.

Minimalne użycie:

```csharp
var client = new StandPulseSensorClient();
var current = await client.GetStatusAsync();

await foreach (var snapshot in client.WatchAsync())
{
  Console.WriteLine(snapshot.Status);
}
```

## Konfiguracja

Konfiguracja hosta obejmuje co najmniej:

1. nazwę portu COM,
2. prędkość portu,
3. maksymalną odległość obecności przy biurku,
4. czas stabilizacji zmiany stanu,
5. czas po którym brak poprawnej próbki oznacza `sensorNotAvailable`.

Przykładowa struktura:

```json
{
  "serial": {
    "portName": "COM4",
    "baudRate": 115200
  },
  "presence": {
    "maxDistanceCm": 120,
    "stateChangeDelaySeconds": 10,
    "sampleTimeoutSeconds": 5
  }
}
```

## Relacja do ScannerViewer

`StandPulse.ScannerViewer` pozostaje narzędziem diagnostycznym do rozpoznawania charakterystyki pracy sensora. Czyta port COM bezpośrednio i nie jest klientem `StandPulse.SensorHost`.

## Pierwszy zakres implementacji

Pierwszy zakres obejmuje:

1. projekt `src/StandPulse.SensorHost`,
2. odczyt JSON Lines z portu COM,
3. mapowanie surowych próbek na `presence`, `absence`, `sensorNotAvailable` i `error`,
4. stabilizację zmian stanu,
5. named pipe `StandPulse.Sensor`,
6. komendy `get` i `watch`,
7. prosty log tekstowy zdarzeń hosta.

Po tym etapie aplikacje klienckie mogą opierać się na stabilnym statusie obecności bez znajomości szczegółów sensora i named pipe.
