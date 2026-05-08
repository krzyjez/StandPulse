---
name: doc
description: Twórz i aktualizuj trwałe dokumenty Markdown, specyfikacje, dokumentację techniczną i notatki projektowe.
info: Komenda wspólna do pracy nad dokumentami.
implicit-invocation: true
version: 1
modified-date: 2026-05-01
---

# Zasady tworzenia trwałych plików tekstowych

Używaj tych zasad podczas tworzenia lub aktualizowania dokumentów tekstowych, dokumentów Markdown, specyfikacji, notatek projektowych i dokumentacji lub innych dokumentów tekstowych niebędących kodem.

## Zasady ogólne

### Format

Dokumenty powinny być zapisane w formacie Markdown - chyba że użytkownik explicite poprosi o inny format ale domyślnie jest to Markdown. Kodowanie dokumentu to UTF-8 bez BOM.

### Rozmiar dokumentu

Im większy dokument tym trudniej z niego korzystać dlatego włóż wysiłek w to by dokument był zwięzły - ale nie przesadź skomplikowane elementy powinny być sensownie wyjaśnione. Natomiast nie stosuj przykładów w oczywistych przypadkach, nie używaj kodu jeśli to nie jest niezbędne. A co najważniejsze nie powtarzaj treści już raz opisanych w innym miejscu. 

### Nie powtarzaj treści

Unikaj powtarzania tych samych informacji w wielu miejscach. Jeśli informacja jest ważna, umieść ją w najlepszym, najbardziej właściwym miejscu. Jeśli w innej sekcji trzeba do niej wrócić, użyj krótkiego odwołania albo linku.

### Język dokumentu

Dokumenty piszemy w języku polskim. Słowa angielskie używamy wtedy gdy są powszechnie uznawane za standardowe w danej dziedzinie. Przy bardziej specjalistycznych terminach, a zwłaszcza akronimach warto dodać w nawiasie ich rozwinięcie - np. MVP, MVC, POC itp. Nie wstawiaj definicji słownikowej wystarczy rozwinięcie w nawiasie np. MVP (Minimum Viable Product) za pierwszym użyciem.

### Dokument jako samodzielny opis stanu

Dokument ma być samodzielnym, uporządkowanym opisem obowiązującego stanu. Nie ma być zapisem rozmowy, procesu myślenia agenta ani historii dochodzenia do decyzji. Nie powinien odwoływać się do rozmowy z użytkownikiem. Nie powinien wspominać o tym że w przeszłości było inaczej.

Unikaj sformułowań typu:

1. „Początkowo rozważano...”,
2. „Po rozmowie zdecydowano...”,
3. „Użytkownik zmienił zdanie...”,
4. „Najpierw miało być X, ale potem...”,
5. „W tej wersji przyjmujemy...”.

## Pisz w trybie obowiązującym

Dokument zwykle opisuje rzeczywistość jako obowiązującą regułę lub stan docelowy, nawet jeśli opisuje coś co dopiero powstanie.

Unikaj niepotrzebnego trybu planowania:

1. „będzie używać”,
2. „ma być”,
3. „powinno zostać”,
4. „należy rozważyć”,
5. „docelowo ma być”,
6. „planujemy zrobić”.

Preferuj tryb obowiązujący:

1. „System używa...”,
2. „Dokument zawiera...”,
3. „Agent tworzy...”,
4. „Plik opisuje...”,
5. „Moduł odpowiada za...”.

Używaj słowa „powinien” tylko wtedy, gdy dokument naprawdę opisuje zalecenie, a nie obowiązujący stan albo regułę.

## Nie zostawiaj śladów pracy agenta

Nie zostawiaj w dokumencie śladów pracy nad dokumentem.

Unikaj sformułowań typu:

1. „do dopracowania”,
2. „tu można dodać”,
3. „być może”,
4. „warto się zastanowić”,
5. „alternatywnie”,
6. „wersja robocza”.

Jeśli coś jest niepewne, oznacz to jawnie w osobnej sekcji, na przykład:

1. `## Otwarte pytania`,
2. `## Ryzyka`,
3. `## Decyzje do podjęcia`.

## Linki wewnętrzne

Podczas końcowej redakcji sprawdź, czy dokument zyska na wewnętrznych linkach między sekcjami.

Dodawaj linki, gdy:

1. jedna sekcja rozwija lub uzasadnia inną,
2. szczegółowa definicja znajduje się gdzie indziej,
3. przykład znajduje się w innej części dokumentu,
4. czytelnik może potrzebować szybko przejść do powiązanego kontekstu,
5. używasz pojęcia zdefiniowanego w innej części dokumentu.

# Proces tworzenia dokumentu

## Planowanie struktury dokumentu

Potraktuj przygotowanie struktury dokumentu jako odrębne i ważne zadanie. Od struktury zależy w dużej mierze jakość powstającego dokumentu.

Poświęć czas na przemyślenie jak najlepszej organizacji treści. Najpierw utwórz strukturę rozdziałów i podrozdziałów. Przemyśl co chcesz w nich umieścić. 

Po przygotowaniu struktury zastanów się:

1. czy struktura dokumentu jest logiczna,
2. czy rozdziały i podrozdziały są dobrze dobrane,
3. czy jakaś sekcja nie powinna zostać podzielona, przeniesiona albo połączona z inną,
4. czy najważniejsze informacje nie są powtórzone,
5. czy kolejność rozdziałów jest naturalna dla czytelnika,
6. czy tytuły sekcji jasno mówią, co znajduje się w środku.

Po przygotowaniu struktury traktuj ją jako roboczy punkt odniesienia dla dalszego pisania. Dla dużych, wieloczęściowych albo złożonych dokumentów zapisz strukturę w osobnym pliku `<nazwa>.outline.md` obok planowanego dokumentu wynikowego. Po zakończeniu pracy nad dokumentem usuń plik outline.

## Decyzja o trybie tworzenia dużych dokumentów

Jeśli dokument wygląda na duży, użyj trybu wielodokumentowego ze scalaniem od początku pracy (patrz: `doc-multifile.md`).

Jeśli dokument wymaga scalania plików roboczych albo generowania spisu treści, użyj narzędzi opisanych w `doc-tools.md`.

## Dokumentacja programów

Jeśli tworzysz dokumentację programu, biblioteki, API, CLI, modułu technicznego albo systemu, skorzystaj z pliku `doc-reference-explanation.md`, gdzie opisane są zasady tworzenia dokumentacji technicznej.

## Weryfikacja po zakończeniu pracy 

Po napisaniu dokumentu sprawdź jego jakość - potraktuj sprawdzenie jakości jako odrębne zadanie o dużej ważności. Sprawdź:

1. Czy dokument jest długi? Jeśli tak to czy nie warto do niego dodać spisu treści?
2. czy dokument nie opisuje dialogu z użytkownikiem,
3. czy dokument jest napisany jako stan obowiązujący,
4. czy nie ma fragmentów typu „będzie”, „planujemy”, „warto rozważyć”, jeśli nie są celowe,
5. czy dokument da się czytać samodzielnie bez znajomości rozmowy,
6. czy wszystkie linki są poprawne? czy warto dodać jeszcze jakieś linki?
7. czy w dokumencie nie używasz skomplikowanych lub specjalistycznych terminów bez wyjaśnienia ich znaczenia?
