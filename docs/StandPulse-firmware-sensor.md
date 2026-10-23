# StandPulse - firmware sensora

Ten dokument opisuje aktualny firmware dla pierwszego czujnika StandPulse:

```text
24GHz mmWave for XIAO -> XIAO ESP32-C3 -> USB-C -> Windows COM4
```

Aktualnie wgrany program to:

```text
firmware/standpulse_sensor/standpulse_sensor.ino
```

Katalog `firmware` powinien zawierać źródła firmware'u, który faktycznie jest przeznaczony do wgrania na urządzenie. Testowy `xiao_serial_smoke_test` został usunięty, żeby nie mieszał się z docelowym kodem.

## Aktualne pliki

| Plik | Rola |
|---|---|
| `firmware/standpulse_sensor/standpulse_sensor.ino` | Aktualny firmware dla XIAO ESP32-C3. |
| `scripts/read-standpulse-com.ps1` | Pomocniczy skrypt PowerShell do czytania danych z `COM4`. |
| `src/StandPulse.ScannerViewer` | Małe okienko C# / WinForms do obserwacji stanu i odległości z czujnika. |
| `src/StandPulse.ScannerViewer/sensor-settings.json` | Konfiguracja interpretacji sensora: port, baud rate, próg odległości i czas stabilizacji. |

## Dwa różne łącza szeregowe

W projekcie są dwa osobne odcinki komunikacji i dlatego widać dwie różne prędkości.

| Odcinek | Kod / ustawienie | Znaczenie |
|---|---|---|
| Radar -> XIAO | `radarSerial.begin(256000, SERIAL_8N1, D2, D3);` | XIAO czyta radar mmWave po UART na pinach `D2/D3`. `256000` to fabryczna prędkość transmisji radaru. |
| XIAO -> PC | `Serial.begin(115200);` | XIAO wysyła tekstowe linie JSON do Windows przez USB serial / `COM4`. |
| Częstotliwość próbek | `REPORT_INTERVAL_MS = 200` | Firmware wysyła do PC najnowszą ramkę radaru co 200 ms, czyli około 5 próbek na sekundę. |

Ważne: `256000` to **prędkość transmisji UART**, a nie częstotliwość odczytu. Radar nadaje ramki częściej; firmware wysyła tylko najnowszą z każdego okna 200 ms.

## Ramka radaru

Radar nadaje ramki zgodne z protokołem HLK-LD2410. Firmware parsuje je sam, bez `getStatus()` z biblioteki `mmwave_for_xiao`: biblioteka udostępnia tylko jedną odległość, a jej parser kopiuje bufor z błędnym indeksem.

| Bajty | Znaczenie |
|---|---|
| 0-3 | Nagłówek `F4 F3 F2 F1`. |
| 4-5 | Długość danych (little-endian). |
| 6 | Typ: `0x02` dane podstawowe, `0x01` tryb engineering. |
| 7 | `0xAA`. |
| 8 | Stan celu: brak, ruchomy, nieruchomy, oba. |
| 9-10, 11 | Odległość celu ruchomego (cm) i jego energia. |
| 12-13, 14 | Odległość celu nieruchomego (cm) i jego energia. |
| 15-16 | Detection distance (cm) - jedyne pole wystawiane przez bibliotekę. |
| ostatnie 4 | Ogon `F8 F7 F6 F5`. |

## Co wysyła firmware

Firmware wysyła pojedyncze linie JSON, na przykład:

```json
{"present":false,"targetStatus":"NoTarget","distanceCm":272,"movingDistanceCm":80,"movingEnergy":0,"staticDistanceCm":80,"staticEnergy":6,"radarMode":2}
```

Znaczenie pól:

| Pole | Znaczenie |
|---|---|
| `present` | Uproszczony bool: `true` dla `MovingTarget`, `StaticTarget` albo `BothTargets`. |
| `targetStatus` | Etykieta z radaru: `NoTarget`, `MovingTarget`, `StaticTarget`, `BothTargets` albo `Unknown` przy nieznanym kodzie stanu. Brak ramek z radaru firmware zgłasza zdarzeniem `radar_read_error`, a nie wartością `targetStatus`. |
| `distanceCm` | Detection distance z ramki radaru w centymetrach. W pustym pokoju pokazuje około 272 cm, czyli najpewniej ścianę naprzeciw sensora, a nie osobę. Wcześniejsza robocza nazwa `distanceMm` była myląca. |
| `movingDistanceCm`, `movingEnergy` | Odległość (cm) i energia (0-100) celu ruchomego. Bez celu energia jest bliska zera, a odległość zostaje z poprzedniego odczytu. |
| `staticDistanceCm`, `staticEnergy` | Odległość (cm) i energia (0-100) celu nieruchomego. |
| `radarMode` | Tryb raportowania radaru: `2` oznacza podstawowe dane celu, `1` oznacza tryb engineering z dodatkowymi energiami bramek. |

Gdy przez ponad sekundę nie przyjdzie żadna poprawna ramka, firmware co 200 ms wysyła zdarzenie `{"event":"radar_read_error","millis":...,"silenceMs":...,"invalidFrames":...}`. SensorHost po `presence.maxConsecutiveSensorErrors` takich zdarzeniach publikuje status `error`.

## Odczyt danych w Windows

Urządzenie jest widoczne jako `COM4`.

Odczyt w okienku C#:

```powershell
dotnet run --project .\src\StandPulse.ScannerViewer\StandPulse.ScannerViewer.csproj
```

Okienko pokazuje przede wszystkim dwa stany: `OBECNY` i `NIEOBECNY`, dużą odległość w centymetrach oraz historię zmian stanu. Surowe linie JSON nie są pokazywane w UI, żeby ekran nadawał się do spokojnej obserwacji na bocznym monitorze.

Decyzja `OBECNY` w viewerze jest filtrowana:

```text
radar widzi cel && distanceCm < 120
```

Zmiana stanu jest akceptowana dopiero wtedy, gdy nowy kandydat utrzymuje się przez około 10 sekund. Dzięki temu pojedyncze skoki radaru nie powinny natychmiast przełączać widoku.

Te wartości są konfigurowalne w pliku:

```text
sensor-settings.json
```

Domyślnie:

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

Odczyt przez PowerShell:

```powershell
.\scripts\read-standpulse-com.ps1 -PortName COM4 -Seconds 30
```

Skrypt domyślnie używa prędkości `115200`, czyli tej samej, która jest ustawiona w `Serial.begin(115200)`.

## Kompilacja i upload

Firmware kompilował się poprawnie dla płytki:

```text
esp32:esp32:XIAO_ESP32C3
```

Kompilacja:

```powershell
& "C:\Program Files\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe" compile --fqbn esp32:esp32:XIAO_ESP32C3 "firmware\standpulse_sensor"
```

Upload na `COM4`:

```powershell
& "C:\Program Files\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe" upload --fqbn esp32:esp32:XIAO_ESP32C3 --port COM4 "firmware\standpulse_sensor"
```

## Aktualny stan

Potwierdzone:

1. Windows widzi XIAO ESP32-C3 jako `COM4`.
2. Firmware wgrywa się poprawnie na XIAO.
3. XIAO czyta radar przy `256000` baud na sprzętowym UART.
4. PC odbiera linie JSON przez USB serial.

Do sprawdzenia w kolejnym teście:

1. Czy po odejściu od biurka `targetStatus` przechodzi na `NoTarget`.
2. Czy `present` stabilnie zmienia się na `false`.
3. Jak sensownie interpretować `distanceMm` dla realnego ustawienia sensora przy biurku.
