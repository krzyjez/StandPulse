---
name: branch
description: Pomagaj użytkownikowi rozpoczynać albo zakończyć pracę z programem `branch` w bieżącym repozytorium. Używaj także wtedy, gdy użytkownik chce utworzyć nową gałąź git, rozpocząć nową gałąź, założyć branch do zadania, zamknąć gałąź, zakończyć branch albo domknąć workflow brancha.
info: Komenda ułatwia korzystanie z programu `branch`
implicit-invocation: true
version: 12
modified-date: 2026-04-24
---

# Branch

Ta komenda jest warstwą użytkową nad programem CLI `branch`.

Program `branch` traktuj jako komendę dostępną przez `PATH`. Jeśli program nie będzie dostępny, przekonasz się o tym natychmiast z wyniku uruchomienia - poinformuj o tym użytkownika.

Ten dokument zawiera lokalną dokumentację operacyjną programu. Jeśli potrzebujesz dokładnej składni jednej komendy, użyj `branch help <command>` oraz `branch <command> --help`. Możesz też przejrzeć `branch-techdocs.md`.

## Słownik operacyjny

Trzymaj się poniższego słownika operacyjnego:

1. `workflow brancha` - cała praca nad jednym zadaniem od `branch start` do `branch finish`.
2. `krok [agenta]` - pojedynczy okres pracy jednego agenta od `branch step-begin` do `branch step-end`.
4. `agent` - uczestnik workflow identyfikowany imieniem, np. `Mila`.
5. `sesja agenta` - całe okno kontekstowe pracy z agentem identyfikowane imieniem agenta pobieranym przez `branch get-name`

## Idea programu branch

Program `branch` to narzędzie CLI do prowadzenia workflow brancha w jednym repozytorium. Ułatwia użytkownikowi zarządzanie pracą na roboczej gałęzi Git utworzonej dla jednego zadania.

Model pracy zakłada, że:

1. wielu agentów może pracować na jednym branchu,
2. w każdym kroku agenci zapisują (pod swoim imieniem) zmiany jakie dokonali w plikach
3. commit wykonuje się tylko wtedy gdy wszyscy agenci zakończyli swoje kroki
4. jeden commit może agregować kilka kroków różnych agentów gdyż kroki mogły się wcześniej nakładać (overlapped) przez co nie można było zrobić commitu

To jest krótki workflow operacyjny prowadzony razem z użytkownikiem. Skutkiem tego skilla jest wykonanie jednej z komend programu `branch`, z których dwie są najważniejsze w rozmowie z użytkownikiem: `branch start` oraz `branch finish`.

## Stan roboczy

Program zapisuje własny stan w katalogu `.workai` bieżącego repozytorium:

1. `.workai\branch-state.json`,
2. `.workai\branch-state.lock`.

Nie edytuj tych plików ręcznie podczas zwykłej obsługi workflow. Traktuj je jako stan programu `branch`.

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
- Czy chce zwolnić imię agenta? Wtedy wykonaj `branch forget-name <imię>`.
- Czy chce rozpocząć workflow? Wtedy przejdź do **Branch start**.
- Czy chce domknąć workflow? Wtedy przejdź do **Branch finish**.

Dla `start` i `finish` użyj szczegółowych procedur poniżej. Dla pozostałych komend oprzyj się na `branch help <command>` i wyniku programu.

## Branch get-name

`branch get-name [imię]` rezerwuje imię dla sesji agenta. Bez parametru program wybiera pierwsze wolne imię z konfiguracji. Z parametrem próbuje zarezerwować podane imię.

Po udanym wywołaniu zapamiętaj zwrócone imię jako imię agenta w tej sesji.

## Branch forget-name

`branch forget-name <imię>` zwalnia globalną sesję agenta - a imie zwraca do puli wolnych imion. Używaj tej komendy na wyraźne życzenie użytkownika.

Komenda nie domyka workflow brancha i nie zmienia lokalnego stanu `.workai`.

## Branch start

Wywołanie komendy tworzenia gałęzi ma postać:

`branch start "<nazwa>" "<opis>"`

Oznacza to, że w pierwszej kolejności musisz ustalić nazwę i opis gałęzi. Jeśli użytkownik nie poda nazwy gałęzi, pomóż mu ją wymyślić. Nie zgaduj opisu gałęzi na podstawie nazwy, ani odwrotnie. Gdy już ustalicie z użytkownikiem nazwę i opis gałęzi wykonaj `branch start <nazwa> "<opis>"`.

Jeśli program zwróci błąd, nie zgaduj i nie wykonuj dodatkowych operacji Git na własną rękę. Przekaż użytkownikowi problem krótko, a rozwiązanie proponuj tylko wtedy, gdy wynika z komunikatu programu.

Typowe sytuacje problemowe przy `branch start`:

1. Bieżący katalog nie jest repozytorium Git - powiedz, że `branch start` działa tylko w istniejącym repo.
2. W repo istnieje już aktywny workflow - zaproponuj `branch status`, żeby zobaczyć, co jest aktywne.
3. Worktree nie jest czysty - powiedz, że start nowego workflow wymaga czystego stanu repo.
4. Nazwa brancha jest niepoprawna dla Gita - poproś użytkownika o inną nazwę albo zaproponuj prostszą.
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

## Branch finish

Branch finish to komenda zamykająca gałąź. Jej wywołanie to: `branch finish`.
Jak widać nie trzeba żadnych parametrów, więc możesz od razu ją wywołać. To co ona zwróci pokieruje twoją konwersacją z użytkownikiem. Jeśli program zakończy się powodzeniem, poinformuj użytkownika krótko, że branch został domknięty. Jeśli był jakiś problem, przekaż użytkownikowi komunikat programu i zatrzymaj się przed dalszymi działaniami, chyba że program sam podpowiada następny krok.

`branch finish` nie zwalnia imienia agenta. Do tego służy osobna komenda `branch forget-name <imię>`.

Typowe sytuacje problemowe przy `branch finish`:

1. Program `branch` albo `git` nie znajduje się w `PATH` - poinformuj użytkownika i zakończ obsługę skilla.
2. Bieżący katalog nie jest repozytorium Git - powiedz, że `branch finish` działa tylko w repo z aktywnym workflow brancha.
3. Brak aktywnego workflow - powiedz, że nie ma czego domykać; jeśli użytkownik chce zacząć nową pracę, może użyć `branch start`.
4. Aktywny branch nie zgadza się ze stanem workflow - przekaż komunikat programu i nie przełączaj brancha samodzielnie.
5. Istnieje niedomknięty krok agenta - powiedz, że workflow trzeba najpierw domknąć zgodnie z komunikatem programu.
6. Git zwróci błąd przy checkout, squash merge albo commicie zamykającym - przekaż komunikat i zatrzymaj się przed dalszymi działaniami.

### Przebieg rozmowy z użytkownikiem

Tak jak przy `branch start`, rozmowa ma być krótka i operacyjna. Nie opisuj z własnej inicjatywy technicznych szczegółów typu `squash merge`, zapisów stanu workflow albo plików w `.workai`, chyba że użytkownik o to zapyta albo program zwróci błąd i trzeba wyjaśnić problem.

Przykładowy dialog Agenta (A) z użytkownikiem (U):

U: Zakończ branch.

A: Aktywny branch to 'log-fix' - domykam aktywny branch workflow.

A: Gotowe.

## Relacja z krokami agentów

Ten skill służy przede wszystkim do rozmowy z użytkownikiem o `branch start` i `branch finish`.

Obsługa `branch step-begin` i `branch step-end` jest zwykle pilnowana przez reguły pracy agenta, a nie przez ten skill. Agent powinien wiedzieć tylko tyle, że są to komendy rozpoczynania i kończenia kroku agenta, wykonywane przed i po zmianach w plikach.

Jeśli użytkownik wyraźnie pyta o kroki agentów, `flush`, pulę imion, aktywne okno albo powiadomienia, nie zgaduj z pamięci. Użyj `branch help <command>` albo zajrzyj do:

1. `branch-techdocs.md` - krótki techniczny opis programu,
2. `branch-notifier.md` - konfiguracja powiadomień,
3. `branch help <command>` - aktualna składnia konkretnej komendy.

Nie prowadź jednak użytkownika przez te tematy przy zwykłym `branch start` albo `branch finish`, jeśli sam o nie nie pyta.
