---
name: doc-tools
description: Techniczne zasady używania programów `doc-merge` i `doc-toc` podczas pracy nad dużymi dokumentami Markdown.
info: Plik pomocniczy komendy `doc`; używany tylko wtedy, gdy dokument wymaga scalania plików roboczych albo generowania spisu treści.
version: 1
modified-date: 2026-05-01
---

# Doc Tools

Ten dokument opisuje sposób używania programów wspierających tworzenie dużych dokumentów Markdown:

1. `doc-merge`: scala pliki robocze w jeden dokument wynikowy.
2. `doc-toc`: generuje spis treści w miejscu samodzielnej linii markera `<!-- TOC -->`.

To są programy operacyjne używane przez agenta podczas pracy nad dokumentami.

## Wyzwalacze

Użyj tego pliku razem z `doc-cmd.md`, gdy:

1. dokument powstaje z plików roboczych `*.part-NNN.md`,
2. trzeba scalić części dokumentu przez `doc-merge`,
3. dokument ma otrzymać spis treści generowany przez `doc-toc`,
4. trzeba sprawdzić składnię, błędy, kolejność uruchamiania albo kontrakt narzędzi dokumentowych,
5. agent musi poprawić pliki robocze po błędzie `doc-merge` albo `doc-toc`.

## Zakres dokumentu

Ten dokument opisuje techniczne użycie programów `doc-merge` i `doc-toc`: składnię komend, format wejścia i format wyjścia.

Decyzja o pracy w plikach roboczych należy do procesu opisanego w `doc-multifile.md`. Decyzja o dodaniu spisu treści należy do procesu redakcyjnego opisanego w `doc-cmd.md` i `doc-multifile.md`.

## Dostępność programów

Programy `doc-merge` i `doc-toc` są dostępne z `PATH`.

Agent wywołuje je bez szukania plików wykonywalnych w repozytorium i bez zgadywania lokalnych ścieżek do skryptów.

## Komendy wspólne

Każdy program obsługuje dwie komendy informacyjne:

1. `help` - pokazuje opis programu, składnię wywołania, argumenty i podstawowe przykłady.
2. `doc` - zwraca lokalizację pliku ze szczegółową dokumentacją programu albo informację, że dokumentacja nie została znaleziona. Plik dokumentacji ma taką samą nazwę jak program, ale z rozszerzeniem `.md`, i znajduje się w tym samym katalogu co plik wykonywalny programu.

Przykłady:

```powershell
doc-merge help
doc-merge doc
doc-toc help
doc-toc doc
```

Komenda `help` wypisuje instrukcję użycia na standardowe wyjście. Komenda `doc` wypisuje ścieżkę do dokumentacji na standardowe wyjście albo czytelny komunikat, że dokumentacja nie została znaleziona. Program nie szuka dokumentacji w repozytorium ani w katalogu źródeł; sprawdza plik `<nazwa-programu>.md` obok własnego pliku wykonywalnego.

## Układ plików tymczasowych

Pliki robocze są tymczasowymi artefaktami pracy agenta. Agent tworzy je na czas przygotowania dużego dokumentu i uruchamia `doc-merge`, który scala je do pliku wynikowego oraz usuwa po poprawnym scaleniu.

Pliki robocze zapisuj w miejscu planowanego pliku wynikowego, czyli w lokalizacji wskazanej przez użytkownika albo wynikającej z zadania. Ich nazwy składają się z bazowej nazwy dokumentu oraz numeru części:

```text
<katalog>/<nazwa>.part-001.md
<katalog>/<nazwa>.part-002.md
<katalog>/<nazwa>.part-003.md
```

Plik wynikowy powstaje pod nazwą bazową z rozszerzeniem `.md`:

```text
<katalog>/<nazwa>.md
```

 Po poprawnym scaleniu `doc-merge` usuwa pliki `*.part-NNN.md`.

## `doc-merge`

`doc-merge` scala uporządkowane pliki robocze w jeden dokument wynikowy.

Kanoniczne wywołanie:

```powershell
doc-merge "<katalog>/<nazwa>"
```

Komendy informacyjne:

```powershell
doc-merge help
doc-merge doc
```

`doc-merge help` opisuje składnię scalania części. `doc-merge doc` zwraca ścieżkę do pliku `doc-merge.md` znajdującego się w tym samym katalogu co `doc-merge.exe` albo informację, że dokumentacja nie została znaleziona.

Program:

1. przyjmuje bazową nazwę dokumentu bez rozszerzenia `.md`,
2. znajduje pliki pasujące do wzorca `<katalog>/<nazwa>.part-*.md`,
3. sortuje je według numeru `part-NNN`,
4. sprawdza, czy numeracja jest jednoznaczna,
5. scala treść w ustalonej kolejności,
6. zapisuje dokument wynikowy jako `<katalog>/<nazwa>.md`,
7. kończy się błędem, jeżeli nie da się ustalić bezpiecznej kolejności.

Program nie zgaduje brakujących części. Jeżeli istnieją pliki `part-001` i `part-003`, a brakuje `part-002`, zgłasza błąd.

Operacje `doc-merge` są atomowe: jeżeli program zgłosi błąd, nie zmienia plików i zostawia stan sprzed uruchomienia.

## Błędy `doc-merge`

Jeżeli `doc-merge` zgłasza błąd, agent poprawia pliki robocze albo ścieżkę wywołania i uruchamia program ponownie. Nie omijaj błędu przez ręczne sklejenie dużego dokumentu.

Typowe błędy:

1. Brak plików częściowych.
   Program nie znalazł plików pasujących do wzorca `<katalog>/<nazwa>.part-*.md`. Sprawdź bazową nazwę dokumentu i katalog wywołania.
2. Plik wynikowy już istnieje.
   Program nie nadpisuje istniejącego pliku `<katalog>/<nazwa>.md`. Usuń albo przenieś istniejący plik wynikowy, jeżeli scalanie ma zostać wykonane ponownie.
3. Luka w numeracji części.
   Istnieją na przykład `part-001` i `part-003`, ale brakuje `part-002`. Utwórz brakującą część albo popraw numerację istniejących części.
4. Niejednoznaczna numeracja.
   Kilka plików odpowiada tej samej części albo numer części ma niepoprawny format. Uporządkuj nazwy plików do formatu `.part-NNN.md`.
5. Pusty plik części.
   Część robocza istnieje, ale nie zawiera treści. Uzupełnij ją albo usuń i popraw numerację pozostałych części.
6. Link do pliku roboczego prowadzi w próżnię.
   Link między częściami wskazuje plik albo nagłówek, którego nie ma. Popraw link w pliku roboczym albo dodaj brakujący nagłówek przed ponownym scaleniem.
7. Powtarzają się główne nagłówki.
   Po scaleniu kilka rozdziałów ma ten sam nagłówek `#`. To zaburza stabilność głównych linków. Doprecyzuj tytuły głównych rozdziałów w plikach roboczych.

## Linki podczas scalania

`doc-merge` zachowuje albo poprawia linki wewnętrzne.

Linki między plikami roboczymi po scaleniu prowadzą do sekcji w dokumencie wynikowym, a nie do plików `part-*.md`.

Przykład intencji:

```md
[Konfiguracja](./projekt.part-002.md#konfiguracja)
```

po scaleniu staje się linkiem wewnętrznym:

```md
[Konfiguracja](#konfiguracja)
```

Jeżeli `doc-merge` nie umie bezpiecznie poprawić linku do pliku roboczego albo wskazanego nagłówka, zgłasza błąd i nie tworzy pliku wynikowego.

## `doc-toc`

`doc-toc` generuje spis treści w dokumencie Markdown.

Kanoniczne wywołanie:

```powershell
doc-toc --file "<katalog>/<nazwa>.md" --depth 3
```

Komendy informacyjne:

```powershell
doc-toc help
doc-toc doc
```

`doc-toc help` opisuje składnię generowania spisu treści. `doc-toc doc` zwraca ścieżkę do pliku `doc-toc.md` znajdującego się w tym samym katalogu co `doc-toc.exe` albo informację, że dokumentacja nie została znaleziona.

Program:

1. odczytuje nagłówki z dokumentu,
2. generuje linki do sekcji,
3. uwzględnia tylko nagłówki do poziomu wskazanego przez `--depth`,
4. znajduje samodzielną linię markera `<!-- TOC -->`,
5. zastępuje linię markera wygenerowanym spisem treści,
6. zachowuje pozostałą treść dokumentu bez zmian.

Dokument musi zawierać samodzielną linię markera `<!-- TOC -->` w miejscu, w którym program wstawia spis treści. Wzmianka o markerze wewnątrz zdania nie jest traktowana jako miejsce wstawienia spisu treści.

Program nie aktualizuje istniejącego spisu treści. Aby odświeżyć TOC, usuń stary spis treści, wstaw w jego miejsce samodzielną linię markera `<!-- TOC -->` i uruchom `doc-toc` ponownie.

Operacje `doc-toc` są atomowe: jeżeli program zgłosi błąd, nie zmienia pliku i zostawia stan sprzed uruchomienia.

## Błędy `doc-toc`

Jeżeli `doc-toc` zgłasza błąd, agent poprawia dokument albo parametr `--depth` i uruchamia program ponownie.

Typowe błędy:

1. Brak samodzielnej linii markera `<!-- TOC -->`.
   Program nie wie, gdzie wstawić spis treści. Wstaw marker jako osobną linię w docelowym miejscu spisu.
2. Powtarzają się nagłówki objęte parametrem `--depth`.
   Kilka sekcji wygenerowałoby ten sam link w spisie treści. Doprecyzuj nagłówki albo zmień głębokość TOC.
3. Niepoprawny parametr `--depth`.
   Podaj dodatnią liczbę całkowitą odpowiadającą maksymalnemu poziomowi nagłówków w spisie treści.

## Parametr `--depth`

Parametr `--depth` określa najwyższy poziom nagłówków uwzględniany w spisie treści.

Przykłady:

1. `--depth 2` uwzględnia nagłówki `#` i `##`.
2. `--depth 3` uwzględnia nagłówki `#`, `##` i `###`.
3. `--depth 4` uwzględnia nagłówki od `#` do `####`.

## Kolejność uruchamiania programów

Jeżeli dokument powstaje z plików roboczych i ma otrzymać nowy spis treści, kolejność techniczna jest następująca:

1. uruchom `doc-merge`,
2. usuń istniejący spis treści, jeżeli dokument go zawiera,
3. dodaj samodzielną linię markera `<!-- TOC -->` do scalonego dokumentu w miejscu spisu treści,
4. uruchom `doc-toc`,
5. sprawdź techniczny wynik działania programów.

Nie uruchamiaj `doc-toc` przed scaleniem plików roboczych.

Uwaga: pogram toc nie aktualizuje istniejącego spisu treści. Aby odświeżyć TOC, usuń stary spis treści, wstaw w jego miejsce samodzielną linię markera `<!-- TOC -->` i uruchom `doc-toc` ponownie.

## Sprawdzenie po uruchomieniu

Po użyciu `doc-merge` sprawdź:

1. czy plik wynikowy istnieje,
2. czy tymczasowe pliki `*.part-NNN.md` zostały usunięte po udanym scaleniu,
3. czy program nie zgłosił ostrzeżeń albo błędów.

Po użyciu `doc-toc` sprawdź:

1. czy samodzielna linia markera `<!-- TOC -->` została zastąpiona spisem treści,
2. czy program nie zmienił treści poza miejscem linii markera,
3. czy program nie zgłosił ostrzeżeń albo błędów.

## Zasada końcowa

`doc-merge` i `doc-toc` są narzędziami technicznymi. Nie podejmują decyzji redakcyjnych za agenta i nie zastępują procesu opisanego w `doc-cmd.md` oraz `doc-multifile.md`.
