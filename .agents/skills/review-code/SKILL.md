---
name: review-code
description: Użyj, gdy chcesz wykonać krytyczny przegląd zmian w kodzie, całego repozytorium albo wybranego fragmentu projektu. Jeśli zakres nie jest jednoznaczny, agent ma go potwierdzić.
info: Komenda wspólna do rygorystycznego code review z naciskiem na poprawność, architekturę, testy i ryzyka typowe dla kodu generowanego przez AI.
implicit-invocation: false
version: 5
modified-date: 2026-04-29
---


# Review Code

Wykonaj rygorystyczny code review skoncentrowany na poprawności, architekturze, utrzymywalności, jakości testów oraz skrótach charakterystycznych dla kodu generowanego przez AI.

Ten skill jest przeznaczony do ręcznego użycia.
Nie uruchamiaj go automatycznie.
Nie zgaduj zakresu, jeśli użytkownik nie podał go jednoznacznie.
Nie wprowadzaj zmian w plikach podczas review poza zapisaniem pliku wynikowego `Review-*.md`, chyba że użytkownik wyraźnie poprosi o osobny etap napraw.

## Akceptowane formaty zakresu

Użytkownik może podać jeden z poniższych wariantów:

- `git` - przejrzyj tylko niezakomitowane zmiany w repozytorium
- `all` - przejrzyj całe repozytorium
- fragment projektu albo opis aspektu do sprawdzenia, np. `backend`, `obsługa błędów logowania`, `czy cache nie łamie spójności`
  
Jeśli zakres został podany jednoznacznie, nie pytaj o dodatkowe potwierdzenie.
Jeśli zakres nie został podany jednoznacznie, zatrzymaj się i upewnij się, co ma być sprawdzone.
Nie przyjmuj automatycznie, że zakres to `git` tylko dlatego, że istnieją niezakomitowane zmiany.
Domyślną propozycją przy braku zakresu jest `all`, czyli review całego projektu, ale użytkownik powinien mieć szansę potwierdzić albo zawęzić zakres.

Jeśli zakres jest opisany konwersacyjnie albo aspektowo, najpierw własnymi słowami potwierdź, jak rozumiesz zakres review, a potem wykonaj przegląd według tego ustalenia.

## Filozofia review

Bądź krytyczny, a nie pokazowy.

Nie chwal domyślnie.
Nie skupiaj się na drobiazgach, chyba że ukrywają głębszy problem.
Przedkładaj realne ryzyka nad uwagi stylistyczne.
Oceniaj, czy kod jest solidny w kontekście tego projektu, a nie tylko czy „wygląda poprawnie”.

Celem jest wyłapywanie:

* złych decyzji projektowych
* ukrytego sprzężenia
* kruchych założeń
* słabych testów
* słabej obsługi błędów
* przypadkowej złożoności
* skrótów AI, które na pierwszy rzut oka wyglądają poprawnie
* kodu, który działa dziś, ale źle się zestarzeje



## Krok 1: Ustal zakres

### Jeśli zakres to `git`

Przejrzyj tylko niezakomitowane zmiany.

Sprawdź:

* `git status --short`
* `git diff --stat`
* `git diff`

Jeśli nie ma żadnych niezakomitowanych zmian, napisz to jasno.
Jeśli zakres `git` był podany jednoznacznie, zapisz krótki plik review informujący, że nie było zmian do przejrzenia, a potem zakończ.

### Jeśli zakres to `all`

Przejrzyj całe repozytorium.
Nie próbuj czytać wszystkiego bez planu.
Najpierw zmapuj strukturę projektu, potem przejrzyj najważniejsze części.

### Jeśli zakres to jedna lub kilka ścieżek

Przejrzyj tylko te ścieżki.
Traktuj je jako główny zakres review, ale w razie potrzeby sprawdź także pobliskie zależności, aby zrozumieć kontekst.



## Krok 2: Zbierz kontekst projektu zanim zaczniesz oceniać kod

Przed review poszukaj plików opisujących projekt, zasady i pamięć projektu, jeśli istnieją.

Sprawdź pliki takie jak:

* AGENTS.md
* docs/rules
* project-memory.md
* README.md

inne pliki dokumentacyjne (z rozszerzeniem *.md)

Szukaj informacji o:

* zamyśle architektonicznym
* zasadach warstwowania
* konwencjach nazewniczych
* strategii testowania
* oczekiwanym podejściu do obsługi błędów
* konwencjach specyficznych dla technologii
* wcześniejszych decyzjach, których nie należy łamać

Nie zakładaj, że sam kod wszystko wyjaśnia, jeśli istnieje pamięć projektu.



## Krok 3: Wykryj technologię i dobierz właściwą perspektywę

Wykryj stos technologiczny w obrębie wybranego zakresu.

Przykłady:

* TypeScript / JavaScript
* Firebase Functions
* backend Node
* Flutter / Dart
* C# / .NET
* schemat API / OpenAPI
* frameworki testowe używane w projekcie

Najpierw stosuj ogólne kryteria review.
Potem dołóż kryteria specyficzne dla wykrytych technologii.

Jeśli dana technologia, framework albo platforma ma oficjalny, zalecany sposób realizacji danego typu funkcjonalności, preferuj zgodność z tym podejściem zamiast przypadkowego wzorca odtworzonego z pamięci. Traktuj odejście od standardowego podejścia jako problem wtedy, gdy zwiększa kruchość, złożoność albo koszt utrzymania.

Nie wymagaj od użytkownika ręcznego uruchamiania osobnych skilli technologicznych.

## Krok 4: Zdecyduj, czy użyć subagentów

Główny agent sam decyduje, czy w danym review warto użyć subagentów, jeśli pozwalają na to reguły i możliwości bieżącego środowiska pracy.

Użyj subagentów pomocniczych wtedy, gdy zakres review jest wyraźnie podzielony na niezależne obszary, na przykład:

* frontend i backend,
* aplikacja i testy,
* kilka osobnych modułów,
* duży `git diff` obejmujący różne technologie,
* zakres `all`, jeśli repozytorium jest zbyt duże na sensowny pojedynczy przebieg.

Nie używaj subagentów przy małym, lokalnym zakresie, gdzie narzut koordynacji byłby większy niż zysk.

Zasady pracy z subagentami:

* każdy subagent dostaje konkretny, rozłączny obszar do przeczytania,
* subagenci pracują tylko w trybie read-only i nie zmieniają plików,
* subagenci mają zwrócić ryzyka, podejrzane miejsca i brakujące testy, a nie gotowy werdykt całego review,
* główny agent odpowiada za końcową selekcję problemów, kalibrację ważności, numerację, proponowane testy i ostateczny werdykt,
* nie przepisuj mechanicznie wszystkich uwag subagentów; odfiltruj duplikaty, fałszywe alarmy i problemy niskiej wartości.

Jeśli użyjesz subagentów, odnotuj to krótko w sekcji `Wykorzystany kontekst`.

## Krok 5: Wykonaj review według poniższych kryteriów

### A. Poprawność

Sprawdź:

* czy kod naprawdę robi to, co deklaruje
* czy obsłużone są przypadki brzegowe
* czy stany `null`, puste dane, timeouty, retry i błędy są uwzględnione
* czy istnieją ukryte założenia
* czy występują race conditions, założenia co do kolejności albo kruche rozgałęzienia logiki

### B. Architektura i projekt

Sprawdź:

* czy odpowiedzialności są umieszczone we właściwej warstwie
* czy logika biznesowa nie miesza się z transportem, UI, handlerami, kontrolerami albo adapterami
* czy abstrakcje są uzasadnione
* czy projekt można rozwijać bez rozsiewania zmian po całym kodzie
* czy sprzężenie nie jest zbyt wysokie
* czy rozwiązanie pasuje do istniejącej architektury projektu

### C. Utrzymywalność

Sprawdź:

* czy nazwy są precyzyjne i oddają intencję
* czy przepływ kodu jest zrozumiały
* czy występuje duplikacja
* czy kod nie jest nadmiernie zaaabstrahowany albo przeciwnie — zbyt słabo uporządkowany
* czy złożoność jest rzeczywiście potrzebna, a nie przypadkowa
* czy wartości kontraktowe, takie jak powtarzane literały stringowe, klucze, statusy, nazwy pól albo identyfikatory, nie są rozproszone po wielu miejscach zamiast zebrane w jednym sensownym punkcie

### D. Obsługa błędów i obserwowalność

Sprawdź:

* czy błędy nie są połykane, spłaszczane albo źle klasyfikowane
* czy awarie będzie dało się zdiagnozować
* czy logi i diagnostyka są sensowne
* czy ważne porażki nie są ukrywane za mglistymi komunikatami

### E. Testy

Sprawdź:

* czy testy weryfikują zachowanie, a nie szczegóły implementacyjne
* czy obejmują ryzykowne ścieżki, a nie tylko happy path
* czy mocki nie ukrywają problemów integracyjnych
* czy testy naprawdę dowodzą ważnego kontraktu
* czy nie brakuje testów tam, gdzie ryzyko jest realne

### F. Gotowość produkcyjna

Sprawdź:

* kwestie bezpieczeństwa
* ryzyko utraty lub uszkodzenia danych
* kompatybilność wsteczną
* ryzyko migracyjne
* kruchość konfiguracji
* pułapki wydajnościowe
* martwe pola operacyjne, które utrudnią debugowanie lub utrzymanie

### G. Wykrywanie skrótów AI

Szczególnie szukaj:

* kodu, który omija trudny problem zamiast go rozwiązać
* sztucznych abstrakcji dodanych tylko po to, by kod „wyglądał czysto”
* testów, które robią dobre wrażenie, ale niewiele dowodzą
* dziur w implementacji ukrytych za „tymczasowym” kodem
* częściowych implementacji przedstawianych jak kompletne rozwiązania
* wzorców copy-paste ignorujących lokalną architekturę
* kodu klasy demo zamiast klasy produkcyjnej
* rozwiązań sprzecznych z typowym, zalecanym sposobem pracy danego frameworka lub platformy
* rozproszonych magic strings i innych wartości kontraktowych, które powinny być zdefiniowane raz i używane spójnie



## Krok 6: Dodaj perspektywy specyficzne dla technologii

Sprawdź, czy tej komendzie towarzyszą dodatkowe pliki pomocnicze w tym samym katalogu i użyj ich jako dodatkowej perspektywy review.

Jeśli takie pliki istnieją, dopasuj je do wykrytego stacku, na przykład:

- dla C#/.NET użyj `review-code-csharp.md`
- dla Fluttera użyj `review-code-flutter.md`

Jeśli nie istnieją, wykonaj review bez nich i nie zgaduj brakujących kryteriów specyficznych dla technologii.

## Krok 7: Skalibruj wagę problemów

Używaj tych poziomów:

### Krytyczne

Problemy, które mogą powodować:

* niepoprawne działanie
* luki bezpieczeństwa
* uszkodzenie albo utratę danych
* złamane kontrakty
* poważne szkody architektoniczne
* mylące testy dające fałszywe poczucie bezpieczeństwa

### Ważne

Problemy, które nie muszą psuć działania od razu, ale powinny być poprawione przed zaakceptowaniem zmian:

* kruchy projekt
* słaba obsługa błędów
* zły podział na warstwy
* brak ważnych testów
* ryzykowne założenia
* problemy z utrzymywalnością mające realny koszt

### Drobne

Problemy niskiego ryzyka:

* lokalne porządki
* poprawa spójności
* dopracowanie nazewnictwa
* drobne uproszczenia

Nie zawyżaj wagi problemów.
Nie twórz długich list słabych uwag.



## Krok 8: Zapisz wynik review do pliku

Wynikiem pracy review ma być plik Markdown zapisany w katalogu głównym sprawdzanego repozytorium.

Użyj nazwy:

```text
Review-{YYYY-MM-DD}-{zakres}.md
```

Zasady nazwy:

* `{YYYY-MM-DD}` to bieżąca data lokalna.
* `{zakres}` dodaj, jeśli zakres został podany i da się go sensownie skrócić do bezpiecznej nazwy pliku.
* Dla zakresu `git` użyj `Review-{YYYY-MM-DD}-git.md`.
* Dla zakresu `all` użyj `Review-{YYYY-MM-DD}-all.md`.
* Dla ścieżki albo opisu aspektu użyj krótkiego sluga w `kebab-case`, bez ukośników, dwukropków i znaków niebezpiecznych dla Windows.
* Jeśli zakres jest zbyt długi albo nie daje się bezpiecznie zapisać w nazwie, użyj samego `Review-{YYYY-MM-DD}.md`.
* Jeśli plik o tej nazwie już istnieje, nie nadpisuj go. Dodaj kolejny numer, np. `Review-{YYYY-MM-DD}-git-2.md`.

Po zapisaniu pliku w odpowiedzi na czacie podaj krótkie podsumowanie, werdykt i ścieżkę do pliku review.



## Krok 9: Zwróć review zwykłym, ludzkim językiem

Wynik zapisz w tym formacie:

### Problemy krytyczne

Uwzględniaj tylko problemy, które naprawdę trzeba naprawić teraz.
Wypisz je od najważniejszych do najmniej ważnych i ponumeruj.

### Ważne problemy

Uwzględniaj problemy, które powinny być poprawione przed zaakceptowaniem zmian.
Wypisz je od najważniejszych do najmniej ważnych i ponumeruj.

### Drobne uwagi

Dodawaj je tylko wtedy, gdy faktycznie mają wartość.
Wypisz je od najważniejszych do najmniej ważnych i ponumeruj.

### Proponowane testy

Wypisz testy, które warto dodać albo poprawić na podstawie znalezionych problemów i ryzyk.
Uwzględnij szczególnie:

* testy zabezpieczające przed regresją dla znalezionych problemów,
* brakujące testy ważnych ścieżek błędów,
* testy przypadków brzegowych,
* testy integracyjne, jeśli same testy jednostkowe nie dowodzą kontraktu.

Jeśli nie widzisz potrzeby dodawania testów, napisz to jasno i podaj powód.

### Co jest dobre i nie powinno być ruszane

Wymieniaj tylko istotne mocne strony.
Nie wypełniaj tej sekcji ogólnikowymi komplementami.

### Werdykt

Jedno z:

* Gotowe
* Wymaga zmian
* Nie jest gotowe na produkcję

### Zakres review

Napisz dokładnie, co zostało przejrzane:

* niezakomitowane zmiany w git
* całe repozytorium
* konkretne ścieżki
* aspekt albo pytanie review wskazane przez użytkownika

### Wykorzystany kontekst

Krótko opisz, które dokumenty projektowe, pliki pamięci albo notatki architektoniczne zostały użyte.

### Wykryty stack

Wymień tylko to, co ma znaczenie dla danego zakresu.

Dla każdego problemu podaj:

* numer problemu
* dokładną ścieżkę pliku, a gdy to możliwe także linię albo sekcję
* co jest nie tak
* dlaczego to ma znaczenie
* najprostszy rozsądny sposób naprawy

Numeracja problemów ma ułatwiać rozmowę po review.
Używaj jednej globalnej numeracji problemów w całym pliku review.
Nie resetuj numeracji w kolejnych sekcjach ważności.



## Ograniczenia

* Nie przepisuj całego rozwiązania, chyba że użytkownik wyraźnie o to poprosi.
* Nie generuj JSON-a.
* Nie twórz sformalizowanego, maszynowo przetwarzalnego formatu.
* Nie rób review stylu w oderwaniu od projektu i architektury.
* Nie ignoruj pamięci projektu ani ustalonych konwencji.
* Nie zakładaj, że nowy kod jest poprawny tylko dlatego, że testy przechodzą.
* Jeśli zakres jest zbyt szeroki na głębokie review, napisz, które części zostały potraktowane priorytetowo i dlaczego.
* Gdy zasady projektu stoją w sprzeczności z ogólnymi best practices, preferuj zasady projektu, chyba że są wyraźnie szkodliwe.
