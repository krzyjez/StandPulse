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
branch review [--remote <name>]
branch review --complete --body-file <path> --result passed|issues --reviewer <name>
branch finish --message-file <path> [--accept-issues <reason>]
branch fork <fork-name> "<description>" [--no-open]
branch merge <fork-name> --message-file <path> [--accept-issues <reason>]
branch remove <fork-name> [--force]
branch active-window
```

Pełną składnię i bieżące komunikaty sprawdzaj przez `branch help <command>`.

## Imię agenta

Imię agenta pobieraj przed każdym `branch step-begin`, a nie z pamięci - użytkownik może je zmienić w trakcie sesji (w Claude wbudowanym `/rename`, które `agent-signal` przejmuje przy następnej wiadomości):

```powershell
agent-signal get-name <sessionId>
```

W Codexie można pominąć argument tylko przy dostępnej zmiennej CODEX_THREAD_ID. W Claude podaj identyfikator własnej sesji, ustalony z kontekstu sesji lub ścieżki jej transkryptu. Nie wybieraj ID na podstawie ostatniej aktywności w repo; jeśli nie znasz własnego ID, zgłoś brak tej informacji zamiast zgadywać.

Zwrócone imię podaj w:

```powershell
branch step-begin <name>
branch step-end <name> "<description>"
```

Krok zamykaj tym samym imieniem, którym go otwarto, także gdy imię zmieniło się w trakcie kroku.

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

## Review i domknięcie

Użytkownik zleca `branch review` w aplikacji. CLI przygotowuje zakres i ewentualny PR, agent uruchamia subagenta do oceny, a `--complete` przyjmuje jego tekst z pliku poza repo. Nie otwieraj kroku `step-begin` wokół tych wywołań. Samo przygotowanie nie oznacza ukończonego review.

Raporty `.workai/reviews/YYYY-MM-DD-nazwa-galezi-NNN.md` są wersjonowane w Git i zostają po `finish`. Metadane zapisuje CLI jako YAML frontmatter między liniami `---` (`FormatVersion: 2`). Starsze raporty z komentarzem JSON nadal są odczytywane. Request w `.workai/branch-review-request.json` jest lokalnym plikiem pomocniczym. Kolejne rundy zachowują poprzednie raporty; ponowienie rejestracji po błędzie publikacji używa tego samego raportu.

Standardowe adresy HTTPS/SSH `github.com` wybierają PR, pozostałe repozytoria działają lokalnie. Własne hosty Enterprise i aliasy SSH nie są automatycznie rozpoznawane. Kilka różnych celów wymaga `--remote`; rozbieżne fetch/push są odrzucane. Błąd logowania lub sieci nie oznacza lokalnego fallbacku. GitHub wymaga dostępnego `gh` i logowania użytkownika; repo lokalne go nie potrzebuje.

`finish` wymaga aktualnej oceny całego zakresu. Nie wyłącza całego `.workai/reviews/` z porównania: po ocenie dopuszcza tylko jej własny raport. Baza i pozostałe treści muszą odpowiadać review. `issues` wymaga jawnej decyzji użytkownika przekazanej przez `--accept-issues <reason>`; opcja nie pomija nowego kodu ani wymagań GitHuba.

Program sprawdza wynik przed lokalnym scaleniem lub scaleniem PR-a; w obu przypadkach powstaje commit scalający, który zachowuje kroki gałęzi w historii. Ochrona GitHuba nadal obowiązuje, a ręczny merge w przeglądarce nie jest blokowany dodatkowym checkiem agenta. Po scaleniu CLI aktualizuje bazę fast-forward i usuwa lokalny branch. Raporty zostają. Przerwane sprzątanie można ponowić przez `finish`.

## Forki

`branch fork` odbija z bieżącej gałęzi zadanie pomocnicze do osobnego worktree `<dysk>:\.worktrees\<repo>\<nazwa>`, a `branch merge` wraca z nim do rodzica. Obie komendy oraz `branch remove` i `branch finish` działają wyłącznie w katalogu głównym repozytorium — w worktree forka Git nie pozwala przełączyć się na gałąź rodzica.

Relację fork → rodzic trzyma cecha `Kind: fork` w markerze forka oraz wersjonowany plik `.workai/forks.json` na gałęzi rodzica. Dopóki lista nie jest pusta, `branch finish` rodzica odmawia i wypisuje nazwy otwartych forków. Fork ma dokładnie jeden poziom.

W worktree forka obowiązuje zwykły rytm `step-begin` / `step-end` / `flush` / `review`, z jedną różnicą: review jest zawsze lokalne — bez pusha i bez PR-a, nawet w repozytorium powiązanym z GitHubem. Ruch gałęzi rodzica nie unieważnia oceny forka, bo całość po scaleniu przechodzi jeszcze review rodzica przed `finish`. Zmiana kodu samego forka po ocenie nadal wymaga nowego review.

`merge` i `remove` usuwają katalog forka przed wszystkim innym. Gdy okno forka jest otwarte, niczego nie zmieniają i kończą się kodem 1 z prośbą o zamknięcie okna VS Code oraz powtórzenie komendy. Wyjątkiem jest `branch remove <fork> --force`: porzuca fork mimo otwartego okna, odłącza katalog od Gita, a sam katalog kasuje przy kolejnym `branch status`, `fork` albo `remove`. Nazwa spoza `forks.json` zawsze kończy się błędem i niczego nie kasuje. `merge` odmawia też, gdy fork ma niezapisaną pracę — scalenie bierze tylko commity. Po nieudanym commicie scalenia gałąź i wpis zostają, a powtórzony `merge` odtwarza katalog forka.

## Zwięzły raport review

Raport służy użytkownikowi do wychwycenia błędów przed scaleniem. Dokładność przeglądu nie zależy od długości raportu. Przekaż ten kontrakt subagentowi przed oceną:

1. Zacznij od `**Wynik: ...**` i jednego zdania wyjaśniającego, co wymaga poprawy przed scaleniem lub że nie znaleziono problemów. Użyj werdyktu `passed`, `issues` albo `incomplete`, zgodnego z faktycznym zakresem oceny.
2. `**Cel:**` — jedno lub dwa zdania: przegląd ogólny czy konkretnego aspektu, jakie zachowania i ryzyka oceniano. Nie wyliczaj plików, liczby commitów ani statystyk diffu. Ocena jednego aspektu nie zastępuje wymaganego przeglądu całej gałęzi; brak oceny istotnej części zakresu oznacza `incomplete`.
3. `## Problemy` — numerowana lista od najważniejszych. Każda uwaga zawiera wagę, rodzaj (`strategiczne` albo `techniczne`), krótki tytuł, warunek wystąpienia i skutek, link do konkretnego miejsca oraz kierunek naprawy. Zachowaj dowód potrzebny do zrozumienia błędu; odróżniaj odtworzenie problemu od wniosku z kodu. Nie dodawaj kosmetycznych uwag, ogólnych zaleceń ani minimalnej liczby znalezisk. Gdy problemów nie ma, pomiń sekcję.
4. `## Sprawdzenia i ograniczenia` — krótko podaj faktyczne wyniki weryfikacji i luki wpływające na pewność oceny. Wymień nazwy zastosowanych list kontrolnych (technologicznych i `docs/rules` projektu), jeśli zlecenie je podało. Opisz pominięte obszary ich funkcją, bez spisu plików. Istotne ograniczenie konkretnej uwagi umieść przy niej, bez powtarzania go tutaj.

Rodzaj uwagi mówi, kto ją rozstrzyga, i jest niezależny od wagi:

- `strategiczne` — utknięcie, wada algorytmu, zły kierunek rozwiązania, sprzeczne założenia; naprawa wymaga decyzji użytkownika, bo zmienia sposób działania albo rozwoju projektu;
- `techniczne` — konkretny defekt z jasną naprawą w ustalonym już kierunku, na przykład brak warunku wyjścia z pętli, nieobsłużony przypadek brzegowy, nieaktualny komentarz albo błędny odsyłacz.

W razie wątpliwości oznacz uwagę jako `strategiczną`. Waga ma jedną z trzech wartości: `Krytyczne`, `Ważne`, `Drobne` - tej samej skali używa skill `review-code`. Format wiersza: `**Waga · rodzaj — tytuł.**`, na przykład `**Ważne · techniczne — brak warunku wyjścia z pętli.**`.

Rodzaj nie wpływa na werdykt. W wyborze między `passed` a `issues` każda uwaga w sekcji `## Problemy`, także techniczna, oznacza `issues`; `passed` jest zarezerwowane dla raportu bez uwag. Pierwszeństwo ma `incomplete`: jeśli istotna część zakresu nie została oceniona, werdyktem jest `incomplete`, nawet gdy znalazłeś uwagi. Rodzaj decyduje tylko o tym, jak uwagę przedstawia się użytkownikowi i kto ją rozstrzyga.

Nie dodawaj własnego tytułu H1 (tworzy go CLI), sekcji metody, wykazu przeczytanego kontekstu, pochwał ani końcowego powtórzenia werdyktu. SHA, daty, identyfikatory recenzenta i PR należą do YAML tworzonego przez CLI; nie powtarzaj ich w treści. Nie wymagaj wersji pluginu lub modelu w raporcie. Regułę projektu cytuj tylko wtedy, gdy uzasadnia konkretną uwagę.

Główny agent sprawdza zgodność wyniku z oceną subagenta i usuwa powtórzenia przed zapisem. Raporty kilku recenzentów scala w jeden raport według tego kontraktu, bez osobnych sekcji dla każdego recenzenta. Nie zmienia werdyktu, wagi, dowodów ani istotnych ograniczeń w celu skrócenia tekstu. Niejasności wyjaśnia z recenzentem. Samo skrócenie raportu nie upoważnia do uznania niepełnej oceny za ukończoną.

## Podsumowanie dla finish

Podsumowanie dla `finish`: plik UTF-8 z tytułem w pierwszej linii, pustą linią i opisem końcowych zmian oraz weryfikacji. Agent przygotowuje go na podstawie diffu, celu i review, bez komasowania opisów kroków. Wywołanie: `branch finish --message-file <plik>`. Lokalnie wiadomość trafia do `git commit -F` commitu scalającego, a w GitHubie przez plik JSON do API scalenia metodą merge. Brak lub błędny opis blokuje nowe scalanie; pusty workflow i sprzątanie po już wykonanym merge nie wymagają pliku. Przerwanie przed commitem: gdy scalenie jest w toku (`MERGE_HEAD`), można je dokończyć przez `git commit --no-edit` albo przerwać przez `git merge --abort`; same pozostałości bez `MERGE_HEAD` cofa się powrotem na gałąź roboczą (`git checkout -f <gałąź>`), nigdy commitem na bazie. Potem ponawia się finish.
