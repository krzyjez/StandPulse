---
name: doc-reference-explanation
info: Plik pomocniczy komendy `doc`; używany podczas tworzenia dokumentacji technicznej w modelu Reference i Explanation.
version: 1
modified-date: 2026-05-01
---

# Dokumentacja — Reference i Explanation

Używaj tej zasady podczas tworzenia lub aktualizowania dokumentacji technicznej projektu.

Ten projekt używa tylko dwóch trybów dokumentacji z modelu **Diátaxis**:

1. **Reference** — opis programu z zewnątrz.
2. **Explanation** — wyjaśnienie, jak i dlaczego program działa.

Nie twórz sekcji **Tutorial** ani **How-to guide**, chyba że użytkownik poprosi o to wprost.


## Główna zasada

**`Reference` opisuje program od zewnątrz. `Explanation` może opisywać program od środka.**

`Reference` traktuje program jak czarną skrzynkę: opisuje, co program oferuje światu zewnętrznemu i czego potrzebuje od świata zewnętrznego, żeby działać.

`Explanation` wyjaśnia sens, architekturę, decyzje projektowe i istotne mechanizmy wewnętrzne.

## Linkowanie między Reference i Explanation

Rozdziały opisujące ten sam obszar mogą, a często powinny, linkować do siebie nawzajem.

Jeśli element opisany w `Reference` ma szersze wyjaśnienie w `Explanation`, dodaj link z `Reference` do odpowiedniego miejsca w `Explanation`.

Jeśli `Explanation` omawia element, którego pełna struktura, format, parametry lub kontrakt są opisane w `Reference`, nie powielaj całej specyfikacji. Zamiast tego dodaj link do odpowiedniego miejsca w `Reference`.

Przykłady:

1. Endpoint opisany w `reference` może linkować do sekcji w `explanation`, która wyjaśnia, dlaczego endpoint istnieje i jakie decyzje projektowe za nim stoją.
2. Sekcja w `explanation` omawiająca strukturę JSON nie musi powtarzać całego JSON-a, jeśli pełny format jest opisany w `reference`. Wystarczy link do odpowiedniej sekcji Reference.
3. `Contract` w `Reference` może linkować do Explanation, jeśli jakaś zależność środowiskowa wynika z konkretnej decyzji architektonicznej.

Po utworzeniu lub większej aktualizacji obu rozdziałów wykonaj osobną fazę przeglądu linków:

1. sprawdź, które elementy w Reference mają rozwinięcie w Explanation,
2. sprawdź, które wyjaśnienia w Explanation odnoszą się do konkretnych elementów Reference,
3. dodaj brakujące linki w obie strony tam, gdzie realnie pomagają czytelnikowi,
4. unikaj linkowania wszystkiego ze wszystkim — link ma prowadzić do istotnego rozwinięcia albo do źródła faktów.

## Rozdział Reference



Rozdział `Reference` zawiera dwie główne sekcje:


# Reference

## Interaction

Sekcja **Interaction** opisuje, jak użytkownicy, inne programy albo zewnętrzne systemy wchodzą w interakcję z programem.

Umieszczaj tutaj na przykład:

1. endpointy HTTP,
2. metody HTTP,
3. parametry requestów,
4. formaty requestów i response’ów,
5. kody odpowiedzi i błędów,
6. komendy CLI,
7. argumenty i flagi CLI,
8. kody wyjścia programu,
9. pliki wejściowe i wyjściowe,
10. eventy,
11. webhooki,
12. kolejki,
13. protokoły komunikacyjne,
14. publiczne API biblioteki, jeśli dokumentowany element jest biblioteką.

Nie opisuj tutaj klas, prywatnych metod, helperów, warstw aplikacji ani szczegółów implementacji.

### Contract

Sekcja **Contract** opisuje, co musi istnieć w środowisku zewnętrznym, żeby program działał poprawnie.

Umieszczaj tutaj na przykład:

1. wymagane pliki konfiguracyjne,
2. format plików konfiguracyjnych,
3. wymagane zmienne środowiskowe,
4. wymagane katalogi i pliki,
5. strukturę bazy danych,
6. tabele, kolekcje, encje storage,
7. wymagane dane początkowe,
8. wymagane usługi zewnętrzne,
9. wymagane credentials / API keys / connection strings,
10. wymagane uprawnienia,
11. porty,
12. założenia sieciowe,
13. zależności runtime,
14. ograniczenia systemowe,
15. format danych wejściowych, jeśli program zależy od konkretnego formatu.

Contract ma odpowiadać na pytanie:

> Co musi istnieć wokół programu, żeby program mógł przeżyć i poprawnie działać?

### Powtarzalny format Reference

Opisuj podobne elementy w powtarzalnym formacie.

Endpointy, komendy CLI, zmienne środowiskowe, pliki konfiguracyjne, tabele, formaty danych i kody błędów powinny mieć konsekwentne nagłówki i pola.

Nie opisuj jednego endpointu narracyjnie, drugiego tabelą, a trzeciego listą, jeśli należą do tej samej grupy. Czytelnik powinien móc szybko porównywać podobne elementy.

Dla powtarzalnych elementów preferuj stabilny szablon, na przykład:

1. nazwa elementu,
2. krótki opis,
3. wymagane parametry lub warunki,
4. format wejścia,
5. format wyjścia,
6. błędy lub ograniczenia,
7. link do wyjaśnienia, jeśli istnieje.

### Tabele Markdown

Unikaj rozbudowanych tabel Markdown.

Tabele Markdown są dopuszczalne tylko wtedy, gdy są naprawdę proste i czytelne jako plain text. Zwykle oznacza to maksymalnie 3 kolumny oraz krótkie wartości w komórkach.

Nie używaj tabel Markdown dla szerokich struktur, długich opisów, zagnieżdżonych danych ani wielu kolumn. W takich przypadkach preferuj:

1. listy z nagłówkami,
2. osobne podsekcje dla każdego elementu,
3. bloki kodu dla JSON-a, YAML-a lub innych formatów danych,
4. krótkie listy pól pod konkretnym elementem.

Czytelność w plain text jest ważniejsza niż kompaktowy wygląd tabeli w renderowanym Markdownie.

### Walidacja Reference

Po utworzeniu lub aktualizacji pliku `.reference.md` sprawdź, czy programista albo agent AI może znaleźć konkretny fakt w mniej niż 30 sekund.

Sprawdź przykładowo:

1. gdzie jest endpoint, komenda albo format wejściowy,
2. gdzie są wymagane zmienne środowiskowe,
3. gdzie jest opis wymaganych tabel, plików lub usług,
4. gdzie są kody błędów albo zachowanie widoczne z zewnątrz.

Jeśli znalezienie faktu wymaga czytania długiego opisu, popraw strukturę, nagłówki, kolejność sekcji albo format opisu.

## Rozdział Explanation

Rozdział `Explanation` ma strukturę swobodniejszą:

Explanation służy do wyjaśniania sensu, powodów i architektury.

Umieszczaj tutaj na przykład:

1. po co istnieje dany moduł lub funkcjonalność,
2. jaką rolę pełni w systemie,
3. dlaczego zaprojektowano go w taki sposób,
4. jakie były istotne decyzje architektoniczne,
5. jakie kompromisy zostały przyjęte,
6. jakie są ograniczenia,
7. jakie są relacje z innymi częściami systemu,
8. dlaczego Interaction wygląda właśnie tak,
9. dlaczego Contract wymaga takich elementów,
10. ryzyka i konsekwencje przyszłych zmian,
11. istotne klasy lub komponenty, jeśli pomagają wyjaśnić projekt.

Klasy, komponenty i wewnętrzna struktura kodu mogą pojawić się w Explanation, ale tylko wtedy, gdy pomagają zrozumieć działanie systemu albo decyzje projektowe.

### Domyślny wzorzec Explanation

Rozdział `Explanation` może mieć strukturę swobodną. Jeśli jednak nie ma lepszego układu, użyj następującego wzorca:

1. **Kontekst** — jaki problem istnieje i dlaczego ten obszar jest potrzebny.
2. **Główna idea** — jak działa rozwiązanie na poziomie koncepcyjnym.
3. **Decyzje projektowe** — dlaczego wybrano takie podejście.
4. **Alternatywy i trade-offy** — co odrzucono albo jakie są koszty obecnego rozwiązania.
5. **Konsekwencje zmian** — na co uważać przy przyszłej modyfikacji.

Nie traktuj tego wzorca jako obowiązkowego szablonu dla każdego dokumentu. Użyj go wtedy, gdy pomaga uporządkować wyjaśnienie.

### Linki do kodu źródłowego

Jeśli opisujesz konkretny fragment kodu, dodaj link w formacie: `[Klasa X](./path/to/file.py#L10)`. Numer linii jest ważny aby użytkownik nie błądził po całym pliku szukając wskazanego miejsca. Każde użycie nazwy klasy powinno być podlinkowane. 

Jeśli odwołujesz się do technologii, frameworka lub biblioteki, dodaj link do oficjalnej dokumentacji, np. `[FastAPI](https://fastapi.tiangolo.com/)`.

## Zasady pisania

1. Domyślnie twórz dwa rozdziały: `reference` i `explanation`.
2. Nie twórz Tutorial ani How-to guide, chyba że użytkownik poprosi o to wprost.
3. W Reference zawsze używaj sekcji `Interaction` i `Contract`.
4. Nie opisuj klas w Reference.
5. Nie opisuj prywatnych metod w Reference.
6. Nie opisuj struktury kodu w Reference.
7. Reference ma być zwięzłe, faktograficzne i łatwe do szybkiego sprawdzania.
8. Podobne elementy w Reference opisuj w powtarzalnym formacie.
9. Unikaj rozbudowanych tabel Markdown; używaj ich tylko dla prostych przypadków, najlepiej do 3 kolumn.
10. Explanation może zawierać kontekst, decyzje i opis wnętrza systemu.
11. Jeśli Explanation nie ma naturalnej struktury, użyj wzorca: Kontekst, Główna idea, Decyzje projektowe, Alternatywy i trade-offy, Konsekwencje zmian.
12. Linkuj Reference i Explanation, gdy jeden dokument zawiera rozwinięcie albo źródło faktów dla drugiego dokumentu.
13. Nie powielaj w Explanation dużych struktur, tabel, JSON-ów ani kontraktów opisanych w Reference; linkuj do nich.
14. Po utworzeniu lub większej aktualizacji obu dokumentów wykonaj przegląd linków między nimi.
15. Po utworzeniu lub aktualizacji Reference sprawdź, czy konkretny fakt da się znaleźć w mniej niż 30 sekund.
16. Jeśli istniejąca dokumentacja miesza fakty z wyjaśnieniami, rozdziel je między Reference i Explanation.
17. Jeśli istniejąca dokumentacja zawiera instrukcję krok po kroku, nie twórz z niej How-to guide; przenieś trwałe fakty do Reference, a powody i kontekst do Explanation.
18. Optymalizuj dokumentację dla programisty albo agenta AI, który będzie później pracował nad projektem.

## Szybka klasyfikacja treści

Umieszczaj treść w **Interaction**, jeśli odpowiada na pytania:

1. Jak wywołać program?
2. Jakie są endpointy?
3. Jakie są komendy?
4. Jakie parametry przyjmuje?
5. Co zwraca?
6. Jakie błędy są widoczne z zewnątrz?
7. Jak inne systemy komunikują się z tym programem?

Umieszczaj treść w **Contract**, jeśli odpowiada na pytania:

1. Jakiej konfiguracji program wymaga?
2. Jakich plików potrzebuje?
3. Jakich tabel, baz albo kolekcji oczekuje?
4. Jakie usługi zewnętrzne muszą istnieć?
5. Jakie zmienne środowiskowe są wymagane?
6. Jakie dane muszą istnieć przed uruchomieniem?
7. Jakie są wymagania środowiskowe?

Umieszczaj treść w **Explanation**, jeśli odpowiada na pytania:

1. Dlaczego program działa w ten sposób?
2. Dlaczego wybrano taką strukturę?
3. Jaki problem rozwiązuje ten moduł?
4. Jakie są kompromisy?
5. Jakie są ryzyka?
6. Jakie elementy wewnętrzne są ważne dla zrozumienia działania?

