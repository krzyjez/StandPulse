# Branch Techdocs

Techniczna ściąga dla agenta używającego programu `branch`.
Nie opisuje rozmowy z użytkownikiem o `branch start` i `branch finish`; od tego jest `branch-cmd.md`.

## Składnia komend

```powershell
branch help [command]
branch status
branch start <branch-name> "<description>"
branch get-name [name]
branch step-begin <name>
branch step-end <name> "<description>"
branch flush [--force]
branch finish
branch forget-name <name>
branch reset-names
branch active-window
```

Pełną składnię i bieżące komunikaty sprawdzaj przez `branch help <command>`.

## Imię agenta

Agent powinien znać swoje imię z `init`.

Jeśli imienia nie ma w kontekście, użyj awaryjnie:

```powershell
branch get-name
```

Od tego momentu używaj tego samego imienia w:

```powershell
branch step-begin <name>
branch step-end <name> "<description>"
```

Nie używaj `--noname`. `branch finish` nie zwalnia imienia agenta.

## Krok agenta

`branch step-begin <name>` uruchom przed zmianami w plikach.
`branch step-end <name> "<description>"` uruchom po zakończeniu zmian.

Opis w `step-end` ma krótko powiedzieć, co zostało zmienione. Może zawierać listę plików, jeśli to pomaga.

`step-end` nie musi od razu tworzyć commita. Commit kroku może powstać przy następnym `step-begin`, przy `flush` albo przy `finish`.

## Czego agent nie robi

1. Nie edytuj ręcznie plików `.workai`.
2. Nie zaglądaj do globalnego stanu rezerwacji imion.
3. Nie zgaduj imienia agenta.
4. Nie używaj `branch step-begin --noname`.
5. Nie uruchamiaj `branch reset-names`, jeśli użytkownik wyraźnie o to nie poprosi albo komunikat programu tego nie sugeruje.

## Powiadomienia

Po `branch step-end` program może wysłać powiadomienie przez `work-notify.ps1`.
Nie traktuj problemu z powiadomieniem jako błędu zmian w repo, chyba że sama komenda `branch` zakończy się błędem.

Szczegóły konfiguracji powiadomień są w `branch-notifier.md`.

## Źródła prawdy

1. `branch help <command>` - aktualne zachowanie programu.
2. Ten plik - krótka techniczna ściąga dla agenta.
3. `branch-notifier.md` - tylko konfiguracja powiadomień.
