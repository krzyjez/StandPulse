---
name: todo
description: Aktualizuj plik `todo.md` w katalogu głównym projektu, utrzymując luźną, ale uporządkowaną listę otwartych tematów roboczych.
info: Komenda wspólna do utrzymywania spójnych notatek roboczych w pliku `todo.md`.
implicit-invocation: true
version: 5
modified-date: 2026-07-24
---

# Todo

Aktualizuj robocze notatki projektu w pliku `todo.md`.

Ten komponent służy do utrzymywania tematów, do zajęcia się w przyszłości. Może to być zadanie, problem, pytanie, decyzja do potwierdzenia, luźny opis sytuacji albo szkic feature'a.

Co jest istotne w tym pliku:

- przy modyfikacjach zachowuj numerację i dopisuj rzeczy przy właściwym temacie,
- nie wpisuj kodu ani długich wklejek technicznych.

## Pracuj zawsze na jednym pliku

- Używaj pliku `todo.md` w katalogu głównym projektu.
- Jeśli plik nie istnieje, utwórz go pod dokładnie tą nazwą.
- Nie szukaj alternatywnych nazw.

## Co może trafić do `todo.md`

W `todo.md` mogą trafiać różne rodzaje krótkich notatek roboczych, na przykład:

- zadania do zrobienia,
- rzeczy do sprawdzenia,
- problemy i błędy do naprawienia,
- opis sytuacji, która wymaga później decyzji,
- otwarte pytania,
- luźny szkic feature'a lub pomysłu,
- blokery i zależności.

Wpis nie musi być zawsze zapisany jako idealny, konkretny next step. Ważniejsze jest to, żeby temat dało się potem szybko odnaleźć i kontynuować.

## Numeracja i dopiski

Szanuj istniejącą numerację i lokalny styl pliku.

Jeśli nowa rzecz dotyczy już istniejącego tematu:

- nie dopisuj jej na końcu pliku jako osobnego, luźnego wpisu,
- dopisz ją tam, gdzie jest opisany ten temat,
- jeśli potrzeba, użyj lokalnej podnumeracji, na przykład `a)`, `b)`, `c)` albo innego już używanego wariantu.

Nie renumeruj istniejących tematów bez wyraźnej potrzeby.

Najlepiej, żeby cały kontekst jednego problemu albo feature'a był w jednym miejscu.

Przykład:

```md
# 4. Synchronizacja zamówień
a) Sporadycznie pojawiają się duplikaty po retry.
b) Sprawdzić, czy dotyczy to tylko importu ręcznego.
c) @ użytkownik widział ten przypadek także na produkcji.
```

## Notatki użytkownika

Jeśli użytkownik dopisuje własne uwagi po znaku `@` lub w nawiasach `{}` traktuj je jako ważne i zachowuj je.

Zasady:

- nie usuwaj wpisów użytkownika zaczynających się od `@` i w `{}`, chyba że użytkownik wyraźnie tego chce,
- nie przepisuj ich bez potrzeby,
- możesz je porządkować tylko wtedy, gdy zachowujesz ich sens i oznaczenie.

## Czego nie wpisywać

Nie wpisuj:

- gotowego kodu,
- całych klas, metod ani dużych fragmentów plików,
- długich logów i stack trace'y,
- obszernych wklejek z dokumentacji.

Jeśli trzeba zachować techniczny szczegół, streść go krótko własnymi słowami. Zamiast wklejać kod, wskaż plik lub opisz problem.

## Jak utrzymywać plik

- Możesz scalać oczywiste duplikaty.
- Możesz lekko porządkować wpisy, jeśli poprawia to czytelność.
- Nie przebudowuj pliku na siłę do jednego szablonu, jeśli projekt ma już własny styl.
- Możesz zmieniać znaczniki - i zawartość zgodnie z konwencją - patrz rozdział poniżej

## Znaczniki

Znaczniki wypisujemy w nawiasach kwadratowych w temacie, np.:

```md
# 4. [done] Obsługa błędnego emaila
```

Posługujemy się następującymi znacznikami:
- **todo** - temat jest do zrobienia, planowany - stan początkowy tematu, nowy temat trafia z takim znacznikiem.
- **for-test** - temat jest zrealizowany ale wymaga przetestowania - jeśli zmieniamy na ten znacznik to zamiast treści z etapu **todo** wypisujemy wskazówki co należy przetestować. Jeśli zrealizowałeś temat ustawiasz taki status. Nigdy nie przechodź od **todo** do **done** samodzielnie z pominięciem tego statusu.
- **done** - temat wykonany, zrealizowany i przetestowany - jeśli zmieniamy na ten status to należy wykasować wszystkie wpisy w danym temacie i zostawić sam nagłówek. Zmiana na ten status wymaga potwierdzenia użytkownika.
- **rejected** - temat został odrzucony i nie będzie realizowany - powinna znaleźć się uwaga dlaczego
- **postponed** - temat został opóźniony z różnych powodów - dobrze by było gdyby powód został wpisany
- **blocked** - temat został zablokowany z przyczyn zewnętrznych - np. błędu biblioteki i czekamy na jego poprawienie, przyczyna powinna znaleźć się w temacie

## Zasada końcowa

`todo.md` ma pomagać wrócić do otwartego tematu bez gubienia kontekstu i bez rozwlekania pliku.

Jeśli informacja lepiej pasuje do `project-memory.md` albo `version.md`, nie zapisuj jej tutaj.
