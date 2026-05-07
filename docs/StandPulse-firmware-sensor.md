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
| `firmware/standpulse_sensor/standpulse_sensor.ino` | Aktualny firmware wgrany na XIAO ESP32-C3. |
| `scripts/read-standpulse-com.ps1` | Pomocniczy skrypt PowerShell do czytania danych z `COM4`. |

## Dwa różne łącza szeregowe

W projekcie są dwa osobne odcinki komunikacji i dlatego widać dwie różne prędkości.

| Odcinek | Kod / ustawienie | Znaczenie |
|---|---|---|
| Radar -> XIAO | `radarSerial.begin(256000, SERIAL_8N1, D2, D3);` | XIAO czyta radar mmWave po UART na pinach `D2/D3`. `256000` to fabryczna prędkość transmisji radaru. |
| XIAO -> PC | `Serial.begin(115200);` | XIAO wysyła tekstowe linie JSON do Windows przez USB serial / `COM4`. |
| Częstotliwość próbek | `delay(200);` | Firmware wysyła około 5 próbek na sekundę do PC. |

Ważne: `256000` to **prędkość transmisji UART**, a nie częstotliwość odczytu. Częstotliwość wysyłania próbek wynika teraz głównie z `delay(200)`.

## Co wysyła firmware

Firmware wysyła pojedyncze linie JSON, na przykład:

```json
{"present":true,"targetStatus":"StaticTarget","distanceMm":0,"radarMode":2}
```

Znaczenie pól:

| Pole | Znaczenie |
|---|---|
| `present` | Uproszczony bool: `true` dla `MovingTarget`, `StaticTarget` albo `BothTargets`. |
| `targetStatus` | Etykieta z radaru: `NoTarget`, `MovingTarget`, `StaticTarget`, `BothTargets` albo `ErrorFrame`. |
| `distanceMm` | Odległość raportowana przez bibliotekę radaru w milimetrach. To pole trzeba jeszcze zweryfikować w testach fizycznych. |
| `radarMode` | Tryb raportowania radaru. W dotychczasowym odczycie widoczny był tryb `2`. |

## Odczyt danych w Windows

Urządzenie jest widoczne jako `COM4`.

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
