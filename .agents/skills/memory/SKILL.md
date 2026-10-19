---
name: memory
description: Aktualizuj plik `project-memory.md` w katalogu głównym projektu, zapisując trwałe lub półtrwałe decyzje, ważny kontekst, pułapki, źródła prawdy i operacyjne konwencje projektu.
info: Komenda wspólna. Może być kopiowana do projektów bez zmian albo z bardzo małymi lokalnymi doprecyzowaniami.
implicit-invocation: true
version: 2
modified-date: 2026-04-03
---

# Memory

Aktualizuj pamięć projektu w pliku `project-memory.md`.

Ten komponent służy do zapisywania rzeczy, które projekt ma pamiętać dłużej niż jedną sesję. Nie jest to changelog i nie jest to lista TODO.

Jeśli obok tej komendy istnieje plik `memory-example.md`, traktuj go jako przykład oczekiwanego stylu, poziomu konkretu i struktury `project-memory.md`.

## Pracuj zawsze na jednym pliku

- Używaj pliku `project-memory.md` w katalogu głównym projektu.
- Jeśli plik nie istnieje, utwórz go pod dokładnie tą nazwą.
- Nie szukaj alternatywnych nazw.
- Jeśli plik istnieje, ale ma strukturę wyraźnie inną niż oczekiwana, nie restrukturyzuj go po cichu. Najpierw pokaż problem i zaproponuj uporządkowanie dokumentu do obowiązującej struktury.

## Główna zasada selekcji

Zapisuj tylko informacje trwałe albo półtrwałe, czyli takie, które:

- wpływają na przyszłe implementacje,
- zmniejszają ryzyko powtórzenia błędu,
- wyjaśniają nieoczywistą interpretację projektu,
- wskazują źródła prawdy,
- będą potrzebne po resecie kontekstu.

Nie zapisuj rzeczy, które są tylko chwilowym postępem albo prostym raportem z sesji.

## Czego szukać

Do pamięci projektu zwykle należą:

- decyzje architektoniczne,
- reguły domenowe,
- nieoczywiste interpretacje pól, statusów albo zachowań,
- lokalne źródła prawdy i ich priorytet,
- ograniczenia techniczne,
- pułapki, na których AI albo człowiek już się pomylili,
- istotne heurystyki robocze,
- trwałe operacyjne konwencje pracy z projektem.

Jeśli projekt ma osobne dokumenty z regułami, normami albo dokumentację techniczną, traktuj je jako źródło zasad technicznych. Do `project-memory.md` zapisuj tylko wyjątki, trwałe ustalenia, pułapki albo informacje kontekstowe o projekcie.

## Czego nie zapisywać

Nie zapisuj:

- zwykłych postępów implementacyjnych,
- detali, które należą do `version.md`,
- listy zadań do wykonania,
- pełnych standardów kodowania lub pełnych norm jakości, jeśli mają własny dokument,
- gotowego kodu,
- fragmentów kodu,
- pseudokodu,
- długiego streszczenia rozmowy,
- pytań, na które odpowiedź nie jest jeszcze znana,
- rzeczy oczywistych z kodu, jeśli nie niosą dodatkowej wiedzy.

## Struktura `project-memory.md`

Plik powinien mieć strukturę merytoryczną, nie chronologiczną.

Używaj następujących sekcji:

- `## Sources Of Truth`
- `## Architecture`
- `## Domain Rules`
- `## Operational Conventions`
- `## Pitfalls`

Jeśli plik tworzysz od zera, utwórz od razu taki szkielet.

## Jak przypisywać informacje do sekcji

- `Sources Of Truth`
  Informacje o tym, które pliki, dokumenty albo elementy kodu są nadrzędne przy konfliktach.
- `Architecture`
  Budowa systemu, przepływy, granice modułów, odpowiedzialności komponentów.
- `Domain Rules`
  Reguły biznesowe, interpretacje danych, znaczenie statusów, ograniczenia domeny.
- `Operational Conventions`
  Sposób pracy z projektem, środowisko, narzędzia, reguły edycji, operacyjne konwencje.
- `Pitfalls`
  Rzeczy, które już wcześniej prowadziły do błędów, nieporozumień albo złych założeń.

Jeśli informacja pasuje do kilku sekcji, wybierz jedną tę, w której będzie najbardziej użyteczna w przyszłości. Nie duplikuj wpisów.

## Daty i historia

- Główną osią ma być temat, nie data.
- Datę dodawaj tylko wtedy, gdy naprawdę pomaga zrozumieć zmianę decyzji albo kolejność ustaleń.
- Nie zamieniaj `project-memory.md` w dziennik sesji.

## Styl wpisów

- Pisz krótko i konkretnie.
- Preferuj płaskie punkty zamiast długich akapitów.
- Aktualizuj istniejące punkty, jeśli nowa wiedza zastępuje starą.
- Usuwaj lub poprawiaj wpisy, które są już nieaktualne.
- Nie dopisuj nowej wersji informacji obok starej tylko po to, by zachować historię; `project-memory.md` ma przechowywać aktualną wiedzę, a nie log zmian.
- Przy konflikcie z aktualnym kodem lub nowszym ustaleniem nie konserwuj starej informacji tylko dlatego, że była kiedyś zapisana.
- Nie wklejaj kodu do `project-memory.md`.
- Jeśli trwała norma projektu ma własny dokument, nie duplikuj jej tutaj; zapisz tu tylko wyjątek, kontekst albo odwołanie do źródła prawdy.

## Zasada końcowa

`project-memory.md` ma odpowiadać na pytanie: `co trzeba wiedzieć o projekcie, żeby nie wracać do tych samych pomyłek i ustaleń?`
