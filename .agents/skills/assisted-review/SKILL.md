---
name: assisted-review
description: Prowadź iteracyjny, wspomagany przegląd koncepcji, decyzji lub stanu opisanego w jednym pliku tekstowym na podstawie komentarzy użytkownika umieszczonych między znacznikami. Używaj, gdy użytkownik chce merytorycznie omówić każdą uwagę, zatwierdzić rozstrzygnięcie, a dopiero potem zaktualizować dokument.
metadata:
  version: "3"
  modified-date: "2026-07-30"
  source_component: "components/common/assisted-review-cmd.md"
  source_version: "3"
  source_modified_date: "2026-07-30"
---

# Assisted Review

Prowadź pracę w krótkich rundach: najpierw omów merytorycznie komentarze, potem poczekaj na decyzję użytkownika, a dopiero po zgodzie zmień plik.

## Zasada nadrzędna

Dokument jest miejscem pracy nad koncepcją oraz zapisem przyjętych ustaleń, a nie domyślnym celem samym w sobie. Komentarz umieszczony przy fragmencie tekstu wskazuje przede wszystkim miejsce problemu dotyczącego opisywanej koncepcji, decyzji albo stanu. Najpierw odnieś się do tego problemu, a dopiero po jego rozstrzygnięciu zaproponuj sposób zapisania ustalenia w dokumencie.

Traktuj sam tekst jako główny przedmiot uwagi tylko wtedy, gdy komentarz jest jednoznacznie językowy lub strukturalny albo użytkownik wyraźnie prosi wyłącznie o redakcję.

Oceniaj uwagi samodzielnie. Nie zamieniaj ich automatycznie w polecenia przeredagowania tekstu. Wskaż istotne konsekwencje, sprzeczności, warianty i kompromisy, jeśli rzeczywiście występują, oraz przedstaw własną rekomendację.

Pozostań w zakresie wskazanego dokumentu. Nie proponuj ani nie planuj wdrożenia koncepcji w kodzie, konfiguracji, procesie lub innych plikach. Na pracę wdrożeniową przyjdzie czas po zakończeniu pracy nad dokumentem.

## Wejście

Wymagaj wskazania jednego pliku tekstowego. Jeśli użytkownik nie poda ścieżki, poproś o nią przed rozpoczęciem pracy.

Użytkownik może podać własny znacznik otwierający i zamykający komentarz. Jeśli ich nie poda, przyjmij `{` i `}`.

## Sprawdzenie znaczników

Przed interpretacją komentarzy:

1. Użyj `rg` w trybie wyszukiwania tekstu stałego, aby znaleźć wszystkie wystąpienia obu znaczników w pliku.
2. Obejrzyj ich kontekst i ustal, czy plik nie używa tych znaków do innych celów, np. w JSON-ie, kodzie, szablonach, wzorach albo zwykłej treści.
3. Sprawdź, czy komentarze są domknięte i czy ich granice są jednoznaczne.

Jeśli znaczniki kolidują z treścią, nie zgaduj, które wystąpienia są komentarzami. Zaproponuj użytkownikowi krótką alternatywę i poczekaj na wybór. Preferuj parę pojedynczych, rzadkich znaków, np. `⟦` i `⟧`. Jeśli to nie wystarcza, zaproponuj krótką parę podwojoną, np. `<<` i `>>`.

Nie twórz skryptu do wyszukiwania komentarzy. Używaj `rg` i zwykłego odczytu pliku.

## Rola dokumentu i źródło prawdy

Ustal rolę dokumentu:

1. roboczy zapis koncepcji lub decyzji, które są dopiero opracowywane,
2. specyfikacja albo opis przyjętego stanu docelowego,
3. opis istniejącego stanu weryfikowany względem kodu, innego dokumentu albo wskazanego stanu faktycznego,
4. tekst poddawany wyłącznie redakcji językowej lub strukturalnej.

Jeśli rola wynika jasno z polecenia i kontekstu projektu, działaj bez dodatkowego pytania. Jeśli nie jest jasna i wpływa na sposób rozstrzygnięcia uwag, zadaj jedno krótkie pytanie przed analizą komentarzy.

W przypadku roboczej koncepcji lub stanu docelowego traktuj dokument jako aktualny zapis prac, który może się zmienić wskutek komentarzy użytkownika. Nie uznawaj automatycznie istniejącego kodu za nadrzędny wobec projektowanej koncepcji. Gdy dokument opisuje istniejący stan albo użytkownik wskazał inne źródło prawdy, sprawdzaj względem niego każdą uwagę, której rozstrzygnięcie zależy od faktów.

## Runda analizy

Przed każdą rundą ponownie odczytaj aktualną wersję pliku. Wyodrębnij komentarze w kolejności występowania, ustal numer linii, w której zaczyna się każdy komentarz, i w każdej nowej rundzie zacznij numerację od 1. Dla komentarza wielowierszowego podaj zakres linii. Jeśli środowisko obsługuje linki do plików lokalnych, połącz numer linii bezpośrednio z odpowiednim miejscem w pliku.

Dla każdego komentarza przedstaw:

1. **Komentarz (linia lub zakres linii):** dokładna treść komentarza użytkownika.
2. **Problem lub decyzja:** czego merytorycznie dotyczy uwaga, z zaznaczeniem niejasności.
3. **Ocena:** zasadność uwagi oraz istotne konsekwencje, sprzeczności, warianty lub kompromisy.
4. **Rekomendacja:** proponowane rozstrzygnięcie wraz z krótkim uzasadnieniem.
5. **Jak zapisać to w dokumencie:** mały, jednoznaczny plan aktualizacji treści.

Nie twórz sztucznych wariantów ani rozbudowanej analizy, gdy uwaga jest prosta. Dla komentarza jednoznacznie redakcyjnego możesz krótko połączyć ocenę z rekomendacją i przejść bezpośrednio do proponowanej korekty.

Nie polegaj na kolorze jako sposobie odróżniania tych części, ponieważ renderer tekstu może go nie zachować. Używaj stałych, pogrubionych etykiet.

Na tym etapie nie zmieniaj żadnego pliku. Swobodnie omawiaj zmianę koncepcji lub stanu docelowego opisanego przez dokument, ponieważ jest to główny przedmiot pracy. Nie przekształcaj tej analizy w plan późniejszego wdrożenia poza dokumentem.

## Akceptacja

Po przedstawieniu całej rundy poczekaj na odpowiedź użytkownika. Akceptacja dotyczy zarówno merytorycznego rozstrzygnięcia, jak i sposobu zapisania go w dokumencie.

Użytkownik nie musi osobno akceptować każdego punktu. Gdy odniesie się tylko do wybranych punktów i jednocześnie pozwoli działać, uznaj niewymienione propozycje za zaakceptowane. Sam brak odpowiedzi albo odpowiedź bez zgody na edycję nie jest akceptacją.

Uwzględnij poprawki i zastrzeżenia użytkownika przed edycją. Jeśli użytkownik sam naprawił część komentarzy, nie odtwarzaj wcześniejszej wersji.

## Edycja

Bezpośrednio przed zmianami ponownie odczytaj plik, ponieważ użytkownik mógł go zmienić od czasu analizy.

Zapisz w dokumencie tylko zaakceptowane rozstrzygnięcia. Zaktualizuj powiązane fragmenty wyłącznie wtedy, gdy jest to potrzebne do zachowania spójności koncepcji i zostało uwzględnione w zaakceptowanej propozycji. Usuń znaczniki i treść komentarzy, które zostały rozwiązane. Pozostaw bez zmian komentarze nierozstrzygnięte lub odrzucone przez użytkownika. Zachowaj wszystkie niezwiązane zmiany użytkownika i stosuj lokalne reguły repozytorium dotyczące edycji plików. Nie zmieniaj kodu, konfiguracji ani innych dokumentów.

## Weryfikacja

Po edycji:

1. Ponownie wyszukaj znaczniki przez `rg` i ustal, czy pozostały tylko świadomie nierozstrzygnięte komentarze.
2. Sprawdź diff wskazanego pliku i upewnij się, że każda zmiana wynika z zaakceptowanej uwagi.
3. Zachowaj kodowanie i format pliku.
4. Sprawdź, czy poprawiona treść wiernie zapisuje zaakceptowane rozstrzygnięcia i tworzy spójną koncepcję.
5. Jeśli dokument opisuje istniejący stan, sprawdź jego zgodność ze wskazanym źródłem prawdy.
6. Krótko podsumuj wykonane zmiany i pozostałe komentarze.

Jeśli plik nie zawiera komentarzy oznaczonych ustaloną parą znaków, poinformuj o tym i nie edytuj go.
