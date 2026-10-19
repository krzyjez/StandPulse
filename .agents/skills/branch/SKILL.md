---
name: branch
description: Obsługuj branch start, review z subagentem, finish, forki w osobnym worktree (fork, merge, remove) oraz kroki step-begin/step-end. Używaj przy tworzeniu i zamykaniu gałęzi oraz przed rozpoczęciem zadania w aktywnym workflow branch, aby ocenić zgodność zadania z celem gałęzi.
version: 32
modified-date: 2026-10-12
metadata:
  source_component: "components/common/branch-cmd.md"
  source_version: 32
  source_modified_date: 2026-10-12
---

# Branch

Ta komenda jest warstwą użytkową nad programem CLI `branch`.

Program `branch` traktuj jako komendę dostępną przez `PATH`. Jeśli program nie będzie dostępny, przekonasz się o tym natychmiast z wyniku uruchomienia - poinformuj o tym użytkownika.

Ten dokument zawiera lokalną dokumentację operacyjną programu. Jeśli potrzebujesz dokładnej składni jednej komendy, użyj `branch help <command>` oraz `branch <command> --help`. Możesz też przejrzeć `branch-techdocs.md`.

## Słownik operacyjny

Trzymaj się poniższego słownika operacyjnego:

1. `workflow brancha` - cała praca nad jednym zadaniem od `branch start` do `branch finish`.
2. `krok [agenta]` - pojedynczy okres pracy jednego agenta od `branch step-begin` do `branch step-end`.
3. `agent` - uczestnik workflow identyfikowany imieniem, np. `Mila`.
4. `sesja agenta` - całe okno kontekstowe pracy z agentem identyfikowane imieniem agenta pobieranym przez `agent-signal get-name <sessionId>` (szczegóły identyfikacji sesji: `branch-techdocs.md`).

## Idea programu branch

Program `branch` to narzędzie CLI do prowadzenia workflow brancha w jednym repozytorium. Ułatwia użytkownikowi zarządzanie pracą na roboczej gałęzi Git utworzonej dla jednego zadania.

Model pracy zakłada, że:

1. wielu agentów może pracować na jednym branchu,
2. w każdym kroku agenci zapisują (pod swoim imieniem) zmiany jakie dokonali w plikach
3. commit wykonuje się tylko wtedy gdy wszyscy agenci zakończyli swoje kroki
4. jeden commit może agregować kilka kroków różnych agentów gdyż kroki mogły się wcześniej nakładać (overlapped) przez co nie można było zrobić commitu

To jest krótki workflow operacyjny prowadzony razem z użytkownikiem. Skutkiem tego skilla jest wykonanie jednej z komend programu `branch`, z których najważniejsze w rozmowie z użytkownikiem są: `branch start`, `branch review` oraz `branch finish`.

## Stan roboczy

Program zapisuje własny stan w katalogu `.workai` bieżącego repozytorium:

1. `.workai\branch-state.json`,
2. `.workai\branch-state.lock`.

Nie edytuj tych plików ręcznie podczas zwykłej obsługi workflow. Traktuj je jako stan programu `branch`.

Trwała tożsamość workflow jest dodatkowo zapisana w pustym commicie markera na branchu roboczym. Marker przechodzi przez push i pull, natomiast te pliki `.workai` pozostają lokalnym cache sesji agentów i bieżącego kroku.

`.workai` jest katalogiem narzędzia `branch`. Poza lokalnym stanem trzyma dwie rzeczy wersjonowane w Git: rejestr forków `.workai\forks.json` i raporty review `.workai\reviews\`. Program sam je commituje; nie edytuj ich ręcznie.

## Zasady rozmowy z użytkownikiem

- Nie przedstawiaj planu działań technicznych przed prostym wywołaniem programu `branch`.
- Nie używaj sformułowań typu: zakres jest szerszy, zatrzymuję się przed wykonaniem, powód szerszego zakresu, plan działań.
- Nie opisuj technicznych skutków działania programu, takich jak: zapis plików w `.workai`, utworzenie `branch-state.json`, commit startowy, inne wewnętrzne kroki workflow.
- O tych rzeczach mów tylko wtedy, gdy:
  - użytkownik wyraźnie o nie zapyta,
  - program zwróci błąd i trzeba wyjaśnić problem.

## Rozpoznanie intencji

W pierwszej kolejności rozpoznaj, czego użytkownik chce od programu:

- Czy chce zobaczyć listę komend i parametrów? Wtedy wykonaj `branch help`.
- Czy chce zobaczyć stan workflow? Wtedy wykonaj `branch status`.
- Czy chce uzgodnić lokalny stan po checkout albo pull? Wtedy wykonaj `branch reconcile`.
- Czy chce zsynchronizować starszy workflow bez markera? Wtedy przejdź do **Branch sync**.
- Czy chce rozpocząć workflow? Wtedy przejdź do **Branch start**.
- Czy zaczynamy nowe zadanie lub krok w aktywnym workflow? Najpierw przejdź do **Relacja z krokami agentów**.
- Czy chce wykonać review brancha? Wtedy przejdź do **Branch review**.
- Czy chce domknąć workflow? Wtedy przejdź do **Branch finish**.
- Czy w trakcie pracy wyszedł drugi, niezależny problem? Wtedy przejdź do **Branch fork**.

Dla `reconcile`, `sync`, `start`, `review`, `finish` oraz `fork`, `merge` i `remove` użyj szczegółowych procedur poniżej. Dla pozostałych komend oprzyj się na `branch help <command>` i wyniku programu.

## Branch reconcile

`branch reconcile` uzgadnia lokalny cache `.workai\branch-state.json` z markerem osiągalnym z aktualnego brancha Git. Używaj tej komendy po checkout albo pull oraz wtedy, gdy lokalny stan pochodzi z wcześniejszej pracy na innym komputerze.

Komenda może:

1. odtworzyć brakujący lokalny stan z markera,
2. zastąpić czysty, stary stan dotyczący innego workflow,
3. usunąć czysty, nieaktualny stan po powrocie na branch bazowy,
4. pozostawić bez zmian prawidłowy stan bieżącego workflow.

Jeśli zastąpienie stanu mogłoby utracić informacje przy brudnym worktree, program zatrzymuje się. `branch reconcile --clear-sessions` stosuj tylko na wyraźne życzenie użytkownika.

## Branch sync

`branch sync` pozostaje komendą awaryjną dla starszego workflow, który nie ma jeszcze markera Git. Bez `--base` wykonuje tę samą bezpieczną rekoncyliację co `branch reconcile`; nie przepisuje już automatycznie nazwy brancha roboczego na aktualny branch.

Jeśli na aktualnym branchu istnieje marker albo prawidłowy lokalny stan, wykonaj:

`branch sync`

Jeśli workflow nie ma ani markera, ani lokalnego stanu, a użytkownik chce go świadomie odtworzyć, zapytaj: do którego brancha obecna gałąź ma zostać zintegrowana przy `branch finish`? Dopiero po odpowiedzi i przy czystym worktree wykonaj:

`branch sync --base <branch>`

Nie zgaduj wartości `--base`. Git nie przechowuje pewnej informacji o intencji bazowej gałęzi, więc jest to świadoma decyzja użytkownika.

`branch sync --clear-sessions` uruchamiaj tylko na wyraźne życzenie użytkownika albo gdy komunikat programu wskazuje, że lokalne sesje blokują odzyskanie kontroli nad workflow.

## Branch start

Wywołanie komendy tworzenia gałęzi ma postać:

`branch start "<nazwa>" "<opis>"`

Oznacza to, że w pierwszej kolejności musisz ustalić nazwę i opis gałęzi. Jeśli użytkownik nie poda nazwy gałęzi, pomóż mu ją wymyślić.

Nazwa gałęzi jest zawsze po angielsku, w `kebab-case`: tylko litery ASCII, cyfry i myślniki, bez polskich liter i spacji, np. `fix-login-error`. Pilnuj tego, zanim wywołasz program: jeśli użytkownik poda nazwę po polsku albo z polskimi znakami, zaproponuj angielski odpowiednik i poczekaj na akceptację. Program i tak odrzuci nazwę ze znakami spoza ASCII, ale rozmowa o nazwie należy do Ciebie, nie do komunikatu błędu. Opis gałęzi pozostaje po polsku. Nie zgaduj opisu gałęzi na podstawie nazwy, ani odwrotnie. Gdy już ustalicie z użytkownikiem nazwę i opis gałęzi wykonaj `branch start <nazwa> "<opis>"`.

Jeśli program zwróci błąd, nie zgaduj i nie wykonuj dodatkowych operacji Git na własną rękę. Przekaż użytkownikowi problem krótko, a rozwiązanie proponuj tylko wtedy, gdy wynika z komunikatu programu.

Typowe sytuacje problemowe przy `branch start`:

1. Bieżący katalog nie jest repozytorium Git - powiedz, że `branch start` działa tylko w istniejącym repo.
2. W repo istnieje już aktywny workflow - zaproponuj `branch status`, żeby zobaczyć, co jest aktywne.
3. Worktree nie jest czysty - powiedz, że start nowego workflow wymaga czystego stanu repo.
4. Nazwa brancha jest niepoprawna dla Gita albo nie jest angielska (znaki spoza ASCII, spacje) - zaproponuj poprawną angielską nazwę w `kebab-case`.
5. Branch o tej nazwie już istnieje - poproś użytkownika o inną nazwę albo decyzję, co zrobić z istniejącym branchem.
6. Repo jest w stanie detached HEAD albo Git zwróci inny błąd - przekaż komunikat i zatrzymaj się przed dalszymi działaniami.

### Przebieg rozmowy z użytkownikiem

W rozmowie z użytkownikiem nie przedstawiaj szczegółów technicznych, ani nie tłumacz się że zmieniasz takie i takie pliki. Załóż, że użytkownik wie co robi i że wie jak to działa.

Przykładowy dialog Agenta (A) z użytkownikiem (U):

U: Chcę stworzyć nową gałąź.

A: Rozumiem, że chcesz utworzyć nową gałąź w repozytorium Git'a. Skorzystam ze skilla `branch` by pomóc Ci utworzyć nową gałąź. Podaj teraz nazwę i opis gałęzi - jeśli nie masz pomysłu na nazwę gałęzi podaj jej opis a ja pomogę Ci wymyślić jej nazwę.

U: Chcę naprawić błąd w logowaniu

A: Wymyśliłem dla Ciebie nazwę gałęzi: fix-login-error. Czy chcesz ją przyjąć?

U: Tak

A: Ustaliliśmy nazwę gałęzi: fix-login-error. Teraz podaj jej opis.

U: Aplikacja webowa nie loguje się poprawnie - nie widze plików logów w katalogu logs.

A: Ustaliliśmy opis gałęzi: Aplikacja webowa nie loguje się poprawnie - nie widze plików logów w katalogu logs.

A: Czy chcesz kontynuować z tymi ustawieniami?

U: Tak

A: Tworzę gałąź: fix-login-error

A: Gotowe.

Jak widać dialog z użytkownikiem jest bardzo prosty i nie zawiera żadnych szczegółów technicznych. To przykładowy dialog, nie musi on wyglądać identycznie (np. użytkownik może mieć gotową nazwę gałęzi) - chodzi o pokazanie poziomu szczegółowości.

## Branch review

Polecenie użytkownika „branch review” uruchamia cały proces z poziomu aplikacji agenta. Obejmuje przygotowanie zakresu, ocenę subagenta, zapis raportu oraz push i publikację w PR-ze, jeśli repozytorium jest powiązane z GitHubem. Nie oznacza zgody na scalenie ani automatyczne poprawianie znalezionych problemów.

1. Wywołaj `branch review`. CLI zapisuje zamknięte kroki i zwraca metadane zakresu: `HeadCommit`, `BaseCommit`, `MergeBaseCommit`, `ReportPath` oraz ewentualny PR. Jeśli istnieje kilka celów GitHub, ustal z użytkownikiem remote i ponów z `--remote <nazwa>`. Błąd dostępu do GitHuba nie upoważnia do lokalnego merge.
2. W Codexie przeczytaj `branch-review-codex.md`, a w Claude Code `branch-review-claude.md`. Zastosuj właściwą ścieżkę bez wymagania od użytkownika wyboru recenzenta. Przed delegowaniem potwierdź istnienie trzech commitów (`git cat-file -e <SHA>^{commit}` z argumentem cytowanym w PowerShell) oraz zgodność HEAD z `HeadCommit`; przy rozbieżności pozostaw review nieukończone i odśwież zakres przez CLI. Uruchom osobnego subagenta ze świeżym kontekstem. Przekaż mu ścieżkę repozytorium, cel workflow, trzy identyfikatory commitów i instrukcję oceny pełnego zakresu `MergeBaseCommit..HeadCommit`, z uwzględnieniem integracji z `BaseCommit`. Przekaż także wymagania użytkownika i kryteria akceptacji. Subagent czyta reguły projektu, lokalny `init-session` (tylko zbieranie kontekstu, bez zapisów), odpowiednią dokumentację oraz potrzebny kod i testy. Bez `init-session` zbiera kontekst bezpośrednio z dokumentacji. Nie zmienia plików ani stanu Git; zwraca tekst raportu. Jeśli środowisko nie umożliwia delegowania, poinformuj o tym i pozostaw review nieukończone.
3. W Claude Code stosuj kryteria oficjalnego recenzenta zgodnie z jego instrukcją, z listami kontrolnymi technologii i `docs/rules` projektu; wąskich recenzentów dobierz według zakresu, a na polecenie „pełne review” uruchom wszystkich (szczegóły w `branch-review-claude.md`). W Codexie zastosuj kryteria skilla `review-code`, jeśli jest dostępny; w przeciwnym razie przekaż reviewerowi kryteria: poprawność, regresje, bezpieczeństwo, integracja z bazą i adekwatność testów. Każda uwaga wskazuje miejsce, konsekwencję i sugerowaną naprawę. Przekaż recenzentowi kontrakt „Zwięzły raport review” z `branch-techdocs.md`: wynik, opisowy cel, problemy oraz krótkie sprawdzenia i ograniczenia; bez powtarzania metadanych i list przeczytanych plików. Nie oznaczaj nieprzejrzanego zakresu jako gotowego. Recenzent nie deleguje dalej; brak wymaganego dostępu zgłasza jako ograniczenie, bez prób obejścia. Główny agent sprawdza kompletność odpowiedzi i brak nieoczekiwanych zmian HEAD oraz stanu roboczego. Przy zmianach zatrzymaj rejestrację bez automatycznego cofania plików.
4. Brak odpowiedzi, błąd lub werdykt `incomplete` oznacza nieukończone review: nie wywołuj `--complete`. Nie jest to trzeci wynik CLI. Dla ukończonej oceny zapisz zwróconą treść do pliku tymczasowego poza repozytorium. Nie zapisuj ręcznie metadanych ani docelowego raportu. Zarejestruj wynik: `branch review --complete --body-file <plik> --result passed|issues --reviewer <identyfikator-subagenta>`. `passed` oznacza ukończoną ocenę bez żadnych uwag, `issues` oznacza raport z uwagami - niezależnie od ich wagi i rodzaju - wymagającymi poprawki lub świadomej akceptacji użytkownika.
5. CLI zapisuje i commituje raport w `.workai/reviews/YYYY-MM-DD-nazwa-galezi-NNN.md`, z metadanymi YAML frontmatter między liniami `---`. W trybie GitHub publikuje ten commit i ocenę w tym samym PR-ze. Usuń własny plik tymczasowy po skutecznym zapisie. Po błędzie publikacji ponów rejestrację tego samego wyniku; nie twórz drugiego PR-a ręcznie.
6. Jeśli CLI utworzyło nowy PR, uzupełnij jego opis: cel zadania, wynikające z niego zmiany i faktycznie przeprowadzona weryfikacja. Użyj `gh pr edit --repo <repo> <numer> --body-file <plik-tymczasowy>`; przy istniejącym PR-ze zachowaj treści użytkownika. Podaj krótki wynik, link do raportu i PR-a.
7. Zatrzymaj się. Review kończy turę agenta: przedstaw użytkownikowi streszczenie raportu i czekaj na jego decyzję. Nie zaczynaj poprawek ani kolejnego review z własnej inicjatywy.

Streszczenie ma oszczędzić użytkownikowi czytania całego raportu i pozwolić mu skupić się na tym, co wymaga jego decyzji. Podaj werdykt, a potem uwagi w dwóch grupach zgodnych z ich rodzajem (definicje w `branch-techdocs.md`):

1. `strategiczne` — najpierw, każda osobno, z konsekwencją i pytaniem albo wariantami do rozstrzygnięcia;
2. `techniczne` — jedną krótką listą; użytkownik może je zlecić hurtem.

Po decyzji użytkownika poprawki są zwykłymi krokami pracy, po których wykonuje się review ponownie. Nie uruchamiaj `finish` bez polecenia użytkownika.

CLI nie uruchamia modelu AI. Samo `branch review` wykonane w terminalu przygotowuje zakres, ale nie oznacza zakończenia oceny. `--complete` jest technicznym krokiem wykonywanym przez agenta w aplikacji.

Nie otwieraj kroku `step-begin` wokół operacji samego programu `branch` ani pracy subagenta, który tylko czyta kod. Review wymaga zamkniętych kroków. Zwykłe poprawki kodu nadal podlegają `step-begin` / `step-end`.

## Branch finish

Na polecenie użytkownika przygotuj podsumowanie końcowego rezultatu na podstawie celu workflow, faktycznego diffu względem bazy i aktualnego review. Historia kroków jest materiałem pomocniczym: nie sklejaj jej opisów ani nie zastępuj podsumowania listą skróconych commitów. Opisz finalne zachowanie, istotne zmiany i rzeczywistą weryfikację; pomiń porzucone podejścia i kronikę pracy. Zwykle wystarczy krótki tytuł i jeden lub dwa akapity albo kilka punktów. Numer wersji dodaj tylko wtedy, gdy pomaga opisać wynik.

Zapisz podsumowanie jako UTF-8 bez BOM do pliku tymczasowego poza repo: pierwsza linia to tytuł, potem pusta linia i opis. Uruchom `branch finish --message-file <plik>`. CLI wymaga aktualnego review, dodaje odwołanie do niego i używa tego samego podsumowania w lokalnym commicie scalającym oraz przez GitHub PR. Scalenie zachowuje kroki gałęzi w historii bazy. Pusty workflow i sprzątanie po już wykonanym scaleniu nie wymagają podsumowania. Usuń własny plik po sukcesie; po błędzie zachowaj go do ponowienia i odśwież opis, jeśli zmienił się zakres. Gdy wynik `finish` zawiera linię `UWAGA`, przekaż ją użytkownikowi dosłownie.

Jeśli review zawiera uwagi, przedstaw je użytkownikowi. Po jego jawnej decyzji o zaakceptowaniu problemów uruchom `branch finish --message-file <plik> --accept-issues "<uzasadnienie użytkownika>"`. Nie wymyślaj akceptacji w imieniu użytkownika. Opcja zapisuje decyzję w raporcie; nie omija braku review, zmiany kodu lub bazy ani wymagań ustawionych na GitHubie.

Po błędzie zachowaj workflow. Przy braku albo nieaktualnym review wróć do `branch review`. Nie zastępuj błędu GitHuba lokalnym merge. Nie używaj administracyjnego obejścia ochrony brancha. Ponowne `finish` potrafi dokończyć synchronizację i sprzątanie po scaleniu PR-a w przeglądarce lub przerwaniu programu; komunikaty o konflikcie wymagają rozwiązania wskazanego problemu.

Wyjątek Windows: jeśli workflow zmienia uruchamiany `branch.exe`, skopiuj go do katalogu tymczasowego i wykonaj `finish` z kopii, aby checkout mógł podmienić binarkę. Po zakończeniu usuń własną kopię.

Raporty pozostają w `.workai/reviews/` i w historii Git. `branch finish` nie zwalnia imienia agenta. Po sukcesie krótko poinformuj o domknięciu workflow.

## Branch fork

Fork służy jednej sytuacji: podczas pracy nad zadaniem wychodzi drugi, niezależny problem, którym trzeba zająć się od razu, a którego nie chcesz mieszać z bieżącą gałęzią. Gdy ją rozpoznasz, zaproponuj fork zamiast rozszerzać zakres gałęzi (patrz **Relacja z krokami agentów**, punkt 2). Zadanie całkowicie niezależne od bieżącej pracy nie jest forkiem — zaczyna się zwykłym `branch start` z gałęzi bazowej.

Decyzja należy do użytkownika; `fork`, `merge` i `remove` uruchamiaj tylko na jego wyraźne polecenie. Wszystkie trzy działają w katalogu głównym repozytorium.

1. Ustal z użytkownikiem nazwę i opis forka tak samo jak przy **Branch start** — nazwa po angielsku, w `kebab-case`, bez ukośnika. Wykonaj `branch fork <nazwa> "<opis>"`. Program utworzy gałąź i osobny katalog roboczy oraz otworzy dla niego nowe okno.
2. Powiedz użytkownikowi, że w nowym oknie tworzy agenta przyciskiem `new-session`, tak samo jak w oknie głównym. Ty zostajesz przy swoim zadaniu i nie zajmujesz się pracą forka.
3. Gdy fork jest gotowy i oceniony, użytkownik scala go poleceniem `branch merge <nazwa>`. Przygotuj podsumowanie dokładnie tak jak dla **Branch finish** i przekaż je przez `--message-file`; uwagi review obsłuż przez `--accept-issues` na tych samych zasadach.
4. Fork porzucony bez scalania znika razem z pracą: `branch remove <nazwa>` kasuje katalog roboczy, gałąź i commity. Uruchamiaj tę komendę wyłącznie na jednoznaczne polecenie użytkownika i upewnij się, że rozumie, iż zmian nie da się odzyskać.
5. Gdy program zgłosi, że nie może usunąć katalogu forka, powiedz użytkownikowi wprost: okno VS Code z forkiem jest najpewniej otwarte, trzeba je zamknąć. Nic nie zostało scalone ani usunięte; po zamknięciu okna powtórz tę samą komendę. Otwarte okno bywa niedopatrzeniem, więc przy `remove` najpierw daj użytkownikowi szansę je zamknąć. Gdy odpowie, że nie może tego zrobić (np. pracuje zdalnie), sam uruchom `branch remove <nazwa> --force` - nie pytaj ponownie o zgodę, bo polecenie porzucenia już padło, a `--force` niczego więcej nie kasuje. Fork zostaje wtedy porzucony od razu, a jego katalog program skasuje przy pierwszym poleceniu `branch` (status, fork, remove) po zamknięciu okna; przekaż to użytkownikowi. `merge` nie ma trybu wymuszonego - tam trzeba zamknąć okno.
6. Gdy `merge` odmówi z powodu niezapisanej pracy forka, powiedz użytkownikowi, że w oknie forka trzeba dokończyć krok i ponownie wykonać `branch review`. Nie próbuj tego obchodzić.
7. Gdy `merge` zgłosi nieudany commit scalenia, przekaż przyczynę z komunikatu i zaproponuj jej usunięcie; potem powtórz `branch merge`. Program sam odtworzy katalog forka, gałąź i wpis w rejestrze nie giną.

Gdy pracujesz w oknie forka, rozpoznasz to po tym, że `branch status` pokazuje gałąź forka, a katalog roboczy leży w przestrzeni `.worktrees`. Obowiązuje wtedy zwykły rytm pracy — `step-begin`, `step-end`, `flush`, `review` — z dwiema różnicami:

1. Review forka jest lokalne: nie ma pusha ani PR-a, nawet jeśli repozytorium jest powiązane z GitHubem. Zmiany forka trafiają na GitHub razem z gałęzią rodzica.
2. `start`, `fork`, `finish`, `merge` i `remove` nie działają w oknie forka. Jeśli użytkownik o nie poprosi, powiedz, że wykonuje się je w oknie głównym repozytorium.

Gałąź rodzica nie domknie się, dopóki ma otwarty fork — `branch finish` wypisze wtedy jego nazwę i dwie możliwe komendy. Przekaż to użytkownikowi i poczekaj na decyzję, czy fork scalić, czy porzucić.

## Relacja z krokami agentów

Przed `branch step-begin`, gdy rozpoczynasz nowe zadanie, porównaj jego cel z celem aktywnego workflow. Przy pierwszej ocenie albo po zmianie gałęzi odczytaj `branch status`: kieruj się `Start description` i ustaleniami rozmowy, nie samą nazwą brancha. Jeśli nie ma workflow, nie twórz go w ramach tej kontroli; jeśli cel jest niejasny, doprecyzuj go zamiast zgadywać.

1. Poprawki, testy i dokumentacja potrzebne do wykonania pierwotnego zadania mieszczą się w jego zakresie. Kontynuuj bez dodatkowego pytania; nie oceniaj każdego drobnego kroku od nowa.
2. Przy wyraźnie odrębnym zadaniu, przed otwarciem kroku i zmianami, krótko wskaż rozbieżność: „Ten branch dotyczy logowania, a teraz zaczynamy eksport raportów. Może warto domknąć go przez review i finish, a eksport zacząć na nowym?”. Zapytaj, czy rozdzielamy pracę, czy świadomie rozszerzamy zakres, i poczekaj na decyzję. Jeśli nowy temat trzeba załatwić od razu, a bieżącej gałęzi nie da się jeszcze domknąć, zaproponuj jako trzecią możliwość fork (patrz **Branch fork**).
3. Uwzględniaj już udzieloną zgodę na rozszerzenie zakresu. Zapamiętaj ją w kontekście tej sesji i nie ponawiaj ostrzeżenia dla zaakceptowanego zadania. Wróć do oceny przy kolejnym odrębnym zadaniu; nie zmieniaj ręcznie stanu workflow.
4. Samo wykrycie rozbieżności nie upoważnia do review, finish, przełączenia ani utworzenia gałęzi. Po decyzji wykonaj właściwy workflow; gdy użytkownik pozwoli kontynuować, użyj zwykłego `step-begin` / `step-end`.

Jeśli użytkownik wyraźnie pyta o kroki agentów, `flush`, `reconcile`, `sync`, aktywne okno albo powiadomienia, nie zgaduj z pamięci. Użyj `branch help <command>` albo zajrzyj do:

1. `branch-techdocs.md` - krótki techniczny opis programu,
2. `branch-notifier.md` - konfiguracja powiadomień,
3. `branch help <command>` - aktualna składnia konkretnej komendy.

Nie prowadź jednak użytkownika przez te tematy przy zwykłym `branch start` albo `branch finish`, jeśli sam o nie nie pyta.
