# Project Memory

## Sources Of Truth

- `docs/StandPulse-notatki.md` opisuje cel MVP, główne założenia i logikę działania systemu.
- `docs/StandPulse-komunikacja-sensora.md` opisuje decyzję o lokalnej komunikacji przez Windows Named Pipe.
- `docs/StandPulse-firmware-sensor.md` opisuje aktualny firmware, strukturę sensora i bieżące pliki techniczne.
- `firmware/standpulse_sensor/standpulse_sensor.ino` jest źródłem aktualnego firmware'u dla XIAO ESP32-C3.
- `src/StandPulse.ScannerViewer` zawiera aplikację C# / WinForms do obserwacji danych z sensora.

## Architecture

- StandPulse działa lokalnie na Windows i korzysta z czujnika obecności podłączonego przez USB/COM.
- Tylko jeden proces powinien bezpośrednio otwierać port szeregowy sensora; pozostałe lokalne programy C# mają korzystać z Windows Named Pipe.
- Aplikacja `StandPulse.ScannerViewer` jest projektem `.NET 9.0-windows` z WinForms i biblioteką `System.IO.Ports`.

## Domain Rules

- Czujnik obecności jest elementem obowiązkowym; bez niego projekt nie realizuje celu.
- Reset cyklu następuje dopiero po realnej przerwie poza biurkiem przez skonfigurowany minimalny czas.
- Ostrzeżenie ma być widoczne na osobnym małym monitorze, a nie tylko w głównym oknie aplikacji.

## Operational Conventions

- Repozytorium jest prowadzone w Windows i PowerShell.
- Nie traktuj katalogów `dist`, `bin` i `obj` jako źródeł prawdy; to artefakty wynikowe.
- Przy zmianach w C# stosuj lokalne reguły z `src/StandPulse.ScannerViewer/AGENTS.md`.

## Pitfalls

- `COM4` może być otwarty tylko przez jeden proces naraz, dlatego bezpośredni odczyt z portu musi być scentralizowany.
