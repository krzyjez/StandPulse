# Branch Techdocs

Techniczna ściąga dla agenta używającego programu `branch`.
Nie opisuje rozmowy z użytkownikiem o `branch start` i `branch finish`; od tego jest `branch-cmd.md`.

## Składnia komend

```powershell
branch help [command]
branch status
branch reconcile [--clear-sessions]
branch sync [--base <branch>] [--clear-sessions]
branch start <branch-name> "<description>"
branch step-begin <name>
branch step-end <name> "<description>"
branch flush [--force]
branch finish
branch active-window
```

Pełną składnię i bieżące komunikaty sprawdzaj przez `branch help <command>`.

## Imię agenta

Agent powinien znać swoje imię z `init-session`.

Jeśli imienia nie ma w kontekście, użyj awaryjnie:

```powershell
agent-signal get-name
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
2. Nie zaglądaj do globalnego stanu sesji agentów.
3. Nie zgaduj imienia agenta.
4. Nie używaj `branch step-begin --noname`.

## Reconcile i sync

`branch reconcile` jest zwykłym sposobem uzgodnienia lokalnego `.workai\branch-state.json` z markerem zapisanym w historii aktualnego brancha. Uruchamiaj go po checkout albo pull. Odtwarza stan na drugim komputerze, usuwa czysty stan pozostawiony po powrocie na bazę i nigdy nie tworzy układu, w którym branch roboczy jest równy bazowemu.

`branch sync` służy do awaryjnego odzyskania starszego workflow bez markera. Bez parametrów korzysta z tej samej logiki co `reconcile`.

Jeśli stan istnieje, użyj:

```powershell
branch sync
```

Jeśli nie istnieje ani stan, ani marker, zapytaj użytkownika, do którego brancha obecna gałąź ma zostać zintegrowana przy `branch finish`, upewnij się, że worktree jest czysty, a potem użyj:

```powershell
branch sync --base <branch>
```

Nie zgaduj `--base`. To jest decyzja użytkownika.

## Powiadomienia

Po `branch step-end` program może wysłać powiadomienie przez `work-notify.ps1`.
Nie traktuj problemu z powiadomieniem jako błędu zmian w repo, chyba że sama komenda `branch` zakończy się błędem.

Szczegóły konfiguracji powiadomień są w `branch-notifier.md`.

## Źródła prawdy

1. `branch help <command>` - aktualne zachowanie programu.
2. Ten plik - krótka techniczna ściąga dla agenta.
3. `branch-notifier.md` - tylko konfiguracja powiadomień.
