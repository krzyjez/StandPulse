---
name: doc-multifile
info: Plik pomocniczy komendy `doc`; używany podczas tworzenia dużych dokumentów z plików roboczych.
version: 1
modified-date: 2026-05-01
---

# Tworzenie dużych dokumentów z plików roboczych

Używaj tej zasady podczas tworzenia dużych dokumentów tekstowych lub Markdown, zwłaszcza wtedy, gdy użytkownik chce otrzymać jeden finalny plik, ale dokument jest zbyt duży lub zbyt złożony, żeby bezpiecznie tworzyć go od razu jako całość.

Celem jest ułatwienie pracy agentowi i jednoczesne zachowanie jednego, wygodnego dokumentu końcowego dla człowieka.

## Główna idea

Duży dokument może powstawać etapami w kilku mniejszych plikach roboczych, a następnie zostać scalony do jednego pliku wynikowego. 

Celem takiego podziału jest skupienie pracy agenta na mniejszych fragmentach i ograniczenie ilości informacji przetwarzanej naraz. Pliki robocze służą tylko do organizacji pracy agenta. Plik wynikowy jest dokumentem końcowym.

## Kiedy używać plików roboczych

Oceń ryzyko, że dokument będzie duży lub wieloczęściowy.

Użyj plików roboczych, jeśli zachodzi przynajmniej jeden warunek:

1. dokument ma zawierać kilka wyraźnie różnych części, rozdziałów opisujących odrębne zagadnienia.
2. dokument ma zawierać zarówno część faktograficzną, jak i wyjaśniającą,
3. dokument opisuje więcej niż jeden moduł, proces albo obszar systemu,
4. dokument zawiera wiele endpointów, komend, formatów danych, zmiennych środowiskowych, struktur JSON, tabel lub schematów,
5. istnieje ryzyko, że finalny dokument przekroczy znaczny rozmiar.

Nie czekaj, aż plik stanie się bardzo duży. Jeśli zakres wygląda na duży, podziel pracę od początku.

## Szkielet przed pisaniem

Przed utworzeniem plików roboczych zaprojektuj strukturę dokumentu.

Najpierw ustal:

1. główne części dokumentu,
2. planowane rozdziały i podrozdziały,
3. które treści należą do której części roboczej,
4. przewidywaną kolejność części w pliku wynikowym,

Nie zaczynaj pisać pełnej treści, dopóki podział na części robocze nie jest logiczny.

Podział wynika z tematu i funkcji treści, a nie z przypadkowego limitu znaków.

Dla dużych, wieloczęściowych albo złożonych dokumentów zapisz strukturę w osobnym pliku `<nazwa>.outline.md` obok planowanego dokumentu wynikowego. Plik outline nie jest plikiem roboczym dla `doc-merge` i nie podlega scalaniu. Po zakończeniu pracy nad dokumentem agent usuwa plik outline.

## Decyzja o użyciu subagentów

Użyj subagentów, gdy uznasz to za sensowne, a części dokumentu są wyraźnie niezależne tematycznie i mogą powstawać równolegle bez ryzyka niespójności.

Jeśli środowisko pracy nie pozwala na samodzielne użycie subagentów bez jawnej zgody użytkownika, poinformuj użytkownika, że podział pracy między subagentów jest sensowny, i zapytaj o zgodę przed ich użyciem.

Nie używaj subagentów do części silnie zależnych od siebie, wymagających jednej narracji albo wspólnego ciągłego toku wyjaśnienia.

## Nazewnictwo plików roboczych

Pliki robocze zapisuj w sposób jednoznaczny i uporządkowany.

Dla ogólnych dużych dokumentów używaj nazw:

```text
<nazwa>.part-001.md
<nazwa>.part-002.md
<nazwa>.part-003.md
```
Jest to ważne, gdyż proces scalania wykonuje specjalny program.

Pliki robocze zapisuj w miejscu planowanego pliku wynikowego, czyli w lokalizacji wskazanej przez użytkownika albo wynikającej z zadania.

Pliki wejściowe:

```text
<katalog>/<nazwa>.part-001.md
<katalog>/<nazwa>.part-002.md
<katalog>/<nazwa>.part-003.md
```

Plik wynikowy powstający po scaleniu:

```text
<katalog>/<nazwa>.md
```

## Zalecany rozmiar części roboczych

Każdy plik roboczy powinien mieć logiczny zakres tematyczny i być łatwy do samodzielnego przejrzenia.

Orientacyjne progi:

1. do 10–20 KB — bardzo wygodny rozmiar pliku roboczego,
2. 20–40 KB — nadal akceptowalne, jeśli część jest spójna tematycznie,
3. powyżej 40–50 KB — rozważ dalszy podział,
4. powyżej 50 KB — podział jest zwykle wskazany.

Nie dziel dokumentu wyłącznie według liczby kilobajtów. Rozmiar jest sygnałem pomocniczym, a nie główną zasadą.

## Struktura rozdziałów

Jeśli tworzysz pliki cząstkowe, zadbaj o odpowiednią strukturę rozdziałów. Program scalający nie weryfikuje hierarchii nagłówków. Na początku każdego pliku utwórz dokładnie jeden główny nagłówek rozdziału `#`, ponieważ ten nagłówek staje się nagłówkiem rozdziału w scalonym dokumencie.

## Scalanie plików roboczych

Scalanie odbywa się narzędziowo. Agent przygotowuje pliki robocze i uruchamia narzędzie scalające, ale nie skleja ręcznie dużych dokumentów.

Plik wynikowy jest dokumentem końcowym. Pliki robocze są tymczasowe i po udanym scaleniu usuwa je narzędzie scalające.

Techniczny kontrakt scalania, format nazw plików, obsługa błędów i sposób przepisywania linków są opisane w `doc-tools.md`.


## Generowanie spisu treści

Spis treści generuje narzędzie, nie agent ręcznie. Agent decyduje, czy spis treści jest potrzebny i jaką ma mieć głębokość. Nie twórz spisu treści jeśli dokument jest krótki (mniej niż 200-300 linii).

Techniczna procedura generowania nowego spisu treści i odświeżania istniejącego spisu jest opisana w `doc-tools.md`.

## Stabilne linki i nagłówki

W dużych dokumentach unikaj powtarzania takich samych nagłówków na tym samym poziomie.

Nagłówki powinny być na tyle jednoznaczne, żeby linki wewnętrzne były stabilne po scaleniu.

Jeśli podobna nazwa jest potrzebna, doprecyzuj ją kontekstem.

## Redakcja przed scaleniem

Przed scaleniem plików roboczych wykonaj krótką redakcję każdej części.

Sprawdź:

1. czy część ma logiczną strukturę,
2. czy nie powtarza informacji z innych części,
3. czy nagłówki mają poprawną hierarchię,
4. czy nie zawiera fragmentów roboczych typu „do uzupełnienia”, jeśli nie są celowe,
5. czy linki do innych części są potrzebne i poprawne,
6. czy część nadaje się do umieszczenia w dokumencie wynikowym.

## Redakcja po scaleniu

Po scaleniu wykonaj redakcję finalnego dokumentu.

Sprawdź:

1. czy dokument czyta się jako jeden spójny tekst,
2. czy nie ma powtórzeń między scalonymi częściami,
3. czy tytuł główny występuje tylko raz,
4. czy hierarchia nagłówków jest poprawna,
5. czy spis treści istnieje, jeśli dokument jest duży,
6. czy spis treści prowadzi do istniejących sekcji,
7. czy linki między sekcjami działają,
8. czy linki do plików roboczych zostały przekształcone albo usunięte,
9. czy dokument nie zawiera śladów procesu scalania.

## Zalecany pipeline

Stosuj następujący proces:

1. zaprojektuj szkielet finalnego dokumentu,
2. oceń, czy dokument jest duży lub wieloczęściowy,
3. jeśli tak, zapisz strukturę w pliku `<nazwa>.outline.md` obok planowanego dokumentu wynikowego,
4. utwórz logiczne pliki robocze,
5. napisz i zredaguj pliki robocze,
6. sprawdź powtórzenia i linki między częściami,
7. scal pliki robocze narzędziowo,
8. wygeneruj spis treści zgodnie z procedurą opisaną w `doc-tools.md`,
9. wykonaj końcową redakcję scalonego dokumentu,
10. usuń plik `<nazwa>.outline.md`.
