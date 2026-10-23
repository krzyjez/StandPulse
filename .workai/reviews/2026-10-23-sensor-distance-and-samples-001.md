---
ReviewType: branch-review
FormatVersion: 2
Id: 95d9f0e87612405fad3ef1f1e4c2d69e
WorkflowId: 8506a8b244e84154a8604c891c0f659b
BranchName: sensor-distance-and-samples
BaseBranch: main
HeadCommit: 6ebca1e1a80d18e218ecca22df506b1f83d73c58
BaseCommit: 495ecf0a089b8b218b77f13a296b28080d52e725
MergeBaseCommit: 495ecf0a089b8b218b77f13a296b28080d52e725
ReportPath: .workai/reviews/2026-10-23-sensor-distance-and-samples-001.md
CreatedAt: 2026-10-23T09:43:54.8650185+02:00
Result: issues
Reviewer: pr-review-toolkit:code-reviewer
Remote: origin
GitHubRepository: krzyjez/StandPulse
PullRequestNumber: 5
PullRequestUrl: https://github.com/krzyjez/StandPulse/pull/5
---

# Review: sensor-distance-and-samples

<!-- review-body -->
**Wynik: issues** — w kodzie nie znaleziono nic, co blokuje scalenie; zostały dwie drobne uwagi: strumień `samples` może zgubić linię `status` przy przepełnieniu kolejki, a dokumentacja firmware podaje nieaktualne `ErrorFrame`.

**Cel:** przegląd całego zakresu gałęzi bez binariów: poprawność parsera ramek LD2410 w firmware (synchronizacja, limit bufora, długości i offsety pól względem `getStatus()` z biblioteki), wyliczanie i mediana `targetDistanceCm` oraz reguła decyzji, kontrakt i współbieżność komendy `samples` z niezmiennością `get` i `watch`, a także klient przykładowy, SensorProbe, scenariusz fake i zgodność dokumentacji z kodem.

## Problemy

1. **Drobne · techniczne — przy przepełnieniu kolejki `samples` może przepaść linia `status`.**
   - Warunek: klient `samples` przestaje czytać na tyle długo, że zapełni się kolejka 256 wpisów (przy 5 próbkach na sekundę około 50 s plus bufor pipe), np. zawieszone UI wizualizera albo pauza w debuggerze.
   - Mechanizm: próbki i snapshoty statusu trafiają do jednego kanału `DropOldest` ([Program.cs](src/StandPulse.SensorHost/Program.cs#L1385), [Program.cs](src/StandPulse.SensorHost/Program.cs#L1558)), który odrzuca najstarszy wpis bez względu na typ.
   - Skutek: po wznowieniu klient może nie dostać zmiany stabilnego statusu, w tym `sensorNotAvailable` albo `error`, i pokazuje nieaktualny status do następnej zmiany. Dokumentacja mówi tylko o odrzucaniu „próbek” ([StandPulse-sensor-host.md](docs/StandPulse-sensor-host.md#L282)). Wniosek z kodu, nie odtworzony.
   - Kierunek: odrzucać tylko linie `sample` (statusy osobno albo świeży snapshot po przepełnieniu) albo rozłączać klienta, który nie nadąża; poprawić zdanie w `docs/` i `bin/StandPulse.SensorHost/StandPulse.SensorHost.md`.

2. **Drobne · techniczne — dokumentacja firmware nadal podaje `ErrorFrame` jako możliwy `targetStatus`.**
   - Nowy firmware dla kodu stanu spoza 0–3 wysyła `Unknown` ([standpulse_sensor.ino](firmware/standpulse_sensor/standpulse_sensor.ino#L48)), a brak ramek sygnalizuje zdarzeniem `radar_read_error`. Tabela pól ([StandPulse-firmware-sensor.md](docs/StandPulse-firmware-sensor.md#L67)) wprowadza w błąd, a opis `samples` ([StandPulse-sensor-host.md](docs/StandPulse-sensor-host.md#L276)) nie wymienia `unknown`.
   - Kierunek: w tabeli zamienić `ErrorFrame` na `Unknown`, w opisie `samples` dopisać `unknown`.

## Sprawdzenia i ograniczenia

- Firmware: synchronizacja nagłówka wraca poprawnie po bajcie `F4` w środku nagłówka; długość ramki jest sprawdzana względem `MAX_FRAME_SIZE` przed przepełnieniem (ramka engineering 45 B się mieści); `MIN_PAYLOAD_SIZE = 11` pokrywa odczyt pól; offsety zgodne z biblioteką (detection distance w bajtach 15–16); błędna ramka kosztuje najwyżej jedną resynchronizację.
- Host: `get` i `watch` bez zmian kształtu i kanału; pierwsza linia `samples` to snapshot; linia `status` w `samples` ma kształt `watch`; null `targetDistanceCm` daje zawsze `absence`; kolejka mediany używana tylko z wątku sensora; linie bez pól moving/static działają (`targetDistanceCm = distanceCm`).
- Listy kontrolne: `review-code-csharp.md` i reguły C# z `src/StandPulse.ScannerViewer/AGENTS.md`; bez naruszeń wartych zgłoszenia. Sync-over-async w pętli `samples` powtarza istniejący wzorzec `watch` na wątku LongRunning.
- Nie uruchamiano hosta, firmware, skryptów ani `dotnet build`; build, scenariusze fake 11/11 i odczyt na żywo według weryfikacji głównego agenta. Zachowania parsera na żywym radarze recenzent nie sprawdzał.
- Pominięte: binaria w `bin/**`, ScannerViewer oraz świadomie odłożone punkty (próg energii i odejście przy wygasającym celu nieruchomym, brak testów jednostkowych).
