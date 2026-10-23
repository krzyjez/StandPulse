---
ReviewType: branch-review
FormatVersion: 2
Id: 20ba0a2b0a174621b47b0695ad55351c
WorkflowId: 8506a8b244e84154a8604c891c0f659b
BranchName: sensor-distance-and-samples
BaseBranch: main
HeadCommit: 834f20d295d922658e0c77128428a7f824778be3
BaseCommit: 495ecf0a089b8b218b77f13a296b28080d52e725
MergeBaseCommit: 495ecf0a089b8b218b77f13a296b28080d52e725
ReportPath: .workai/reviews/2026-10-23-sensor-distance-and-samples-002.md
CreatedAt: 2026-10-23T09:56:44.2127795+02:00
Result: passed
Reviewer: pr-review-toolkit:code-reviewer
Remote: origin
GitHubRepository: krzyjez/StandPulse
PullRequestNumber: 5
PullRequestUrl: https://github.com/krzyjez/StandPulse/pull/5
---

# Review: sensor-distance-and-samples

<!-- review-body -->
**Wynik: passed** — obie uwagi z raportu 001 są naprawione, poprawka nie wprowadza regresji i w gałęzi nie ma nic, co wymaga zmiany przed scaleniem.

**Cel:** ogólny przegląd całego zakresu gałęzi bez binariów, z naciskiem na poprawkę kolejki `samples`: kolejność zdarzeń w kanale, pamięć hosta przy kliencie, który przestał czytać, poprawność `ChannelReader.Count` w kanale unbounded i zgodność dokumentacji z kodem. Pozostałe obszary (parser LD2410, mediana `targetDistanceCm`, niezmienność `get`/`watch`, SensorProbe, scenariusz fake) przejrzano ponownie pod kątem zmian od raportu 001.

## Sprawdzenia i ograniczenia

- Uwaga 1 z raportu 001 naprawiona: kanał `samples` jest unbounded, próbka trafia do kolejki tylko przy `Reader.Count < 256` ([Program.cs](src/StandPulse.SensorHost/Program.cs#L1392)), snapshot statusu zawsze ([Program.cs](src/StandPulse.SensorHost/Program.cs#L1558)). Jeden kanał FIFO zachowuje kolejność; przy pełnej kolejce przepadają tylko nowe próbki. `UnboundedChannel` ma `CanCount = true`; ewentualny wyścig z publikacją statusu przekracza limit najwyżej o kilka wpisów. Statusy rosną tylko przy faktycznych zmianach stanu, jak w istniejącym kanale `watch`. Po wznowieniu klient dostaje najpierw stare próbki (rozpoznawalne po `at`), zgodnie z dokumentacją.
- Uwaga 2 z raportu 001 naprawiona: tabela pól firmware podaje `Unknown` i odsyła do zdarzenia `radar_read_error` ([StandPulse-firmware-sensor.md](docs/StandPulse-firmware-sensor.md#L67)); opis `samples` wymienia `unknown` ([StandPulse-sensor-host.md](docs/StandPulse-sensor-host.md#L276)). `ErrorFrame` zostaje tylko w liniach zdarzeń scenariuszy fake sprzed gałęzi, bez wpływu na działanie.
- Listy kontrolne: `review-code-csharp.md` i reguły C# z `src/StandPulse.ScannerViewer/AGENTS.md`; bez naruszeń wartych zgłoszenia.
- Nie uruchamiano hosta, firmware, skryptów ani builda; build, scenariusze fake 11/11 i odczyt na żywym hoście 0.1.8 według weryfikacji głównego agenta. Przepełnienia kolejki nie odtworzono i żaden scenariusz go nie testuje; ocena wynika z kodu.
- Pominięte: binaria w `bin/**`, ScannerViewer oraz świadomie odłożone punkty (próg energii przy wygasającym celu nieruchomym, brak testów jednostkowych).
