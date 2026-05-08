# Project Memory

## Sources Of Truth

- `docs/StandPulse-sensor-host.md` opisuje aktualną architekturę `StandPulse.SensorHost`, named pipe, konfigurację i testy fake sensora.
- `docs/StandPulse-firmware-sensor.md` opisuje aktualny firmware, strukturę sensora i bieżące pliki techniczne.
- `firmware/standpulse_sensor/standpulse_sensor.ino` jest źródłem aktualnego firmware'u dla XIAO ESP32-C3.
- `src/StandPulse.ScannerViewer` zawiera aplikację C# / WinForms do obserwacji danych z sensora.
- `bin/StandPulse.SensorHost` zawiera opublikowany exe hosta, dokumentację użytkową i kopiowalny przykład klienta C#.
- `bin/StandPulse.SensorProbe` zawiera opublikowaną aplikację testową do sprawdzania `get` i `watch` przez named pipe.

## Architecture

- StandPulse działa lokalnie na Windows i korzysta z czujnika obecności podłączonego przez USB/COM.
- Tylko jeden proces powinien bezpośrednio otwierać port szeregowy sensora; pozostałe lokalne programy C# mają korzystać z Windows Named Pipe.
- `StandPulse.SensorProbe` jest klientem testowym i nie otwiera portu COM.
- Aplikacja `StandPulse.ScannerViewer` jest projektem `.NET 9.0-windows` z WinForms i biblioteką `System.IO.Ports`.

## Domain Rules

- Czujnik obecności jest elementem obowiązkowym; bez niego projekt nie realizuje celu.
- Reset cyklu następuje dopiero po realnej przerwie poza biurkiem przez skonfigurowany minimalny czas.
- Ostrzeżenie ma być widoczne na osobnym małym monitorze, a nie tylko w głównym oknie aplikacji.

## Operational Conventions

- Repozytorium jest prowadzone w Windows i PowerShell.
- Nie traktuj katalogów `dist`, `obj` ani `src/**/bin` jako źródeł prawdy; to artefakty wynikowe.
- Rootowy katalog `bin/StandPulse.SensorHost` jest publikowaną paczką hosta i jest częścią repozytorium.
- User PATH powinien zawierać `bin/StandPulse.SensorHost` i `bin/StandPulse.SensorProbe`; autostart użytkownika wskazuje `StandPulse.SensorHost.exe debug`.
- Przy zmianach w C# stosuj lokalne reguły z `src/StandPulse.ScannerViewer/AGENTS.md`.

## Pitfalls

- `COM4` może być otwarty tylko przez jeden proces naraz, dlatego bezpośredni odczyt z portu musi być scentralizowany.
