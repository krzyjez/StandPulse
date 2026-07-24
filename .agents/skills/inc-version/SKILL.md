---
name: inc-version
description: Zwiększ numer wersji projektu robiąc wpis do pliku `version.md`
info: Komenda wspólna. Zakłada ścisły format `version.md`, bo plik może być przetwarzany przez skrypty użytkownika.
implicit-invocation: false
version: 4
modified-date: 2026-07-24
---

# Inc Version

Aktualizuj techniczny changelog projektu w pliku `version.md` znajdującym się w katalogu głównym projektu.

Twoim celem nie jest opisywanie całej sesji, tylko zapisanie najważniejszych zmian implementacyjnych w krótkiej, wersjonowanej formie.

Używaj tej komendy tylko wtedy, gdy zamykany jest sensowny etap pracy, to znaczy stan:

- działający,
- mający nową funkcję albo naprawiony błąd,
- przetestowany albo co najmniej gotowy do sensownego testowania,
- nadający się do oznaczenia wersją.

## Zasady podstawowe

- Pracuj na pliku `version.md` w katalogu głównym projektu.
- Poza `version.md` modyfikuj tylko pliki wymagane przez rozdział `Propagacja wersji`.
- Jeśli plik nie istnieje, utwórz go pod dokładnie tą nazwą.
- Zmieniaj wyłącznie ostatni segment wersji, czyli patch, nie zmieniaj major ani minor - mogą one być zmienione na wyraźne życzenie użytkownika.
- Nowy wpis zawsze wstawiaj na początek pliku.
- Nie modyfikuj wcześniejszych wpisów.
- Nie zgaduj. Jeśli brakuje danych do wpisu, pomiń niepewny punkt i zapytaj użytkownika, czy dana informacja ma trafić do `version.md`.
- Jeśli plik istnieje, najpierw sprawdź czy jego format jest zgodny z przyjętym schematem. Jeśli format jest błędny albo niejednoznaczny, zaraportuj problem i niczego nie modyfikuj.

## Propagacja wersji

Po przygotowaniu nowej wersji w `version.md` sprawdź, czy projekt ma inne jawne miejsce przechowujące numer wersji programu albo pakietu.

Najpierw szukaj istniejącego skryptu lub komendy projektu, która jednoznacznie synchronizuje numer wersji do plików źródłowych. Może to być na przykład skrypt opisany w `package.json`, `pyproject.toml`, pliku projektu, dokumentacji release albo katalogu narzędzi. Jeśli taki skrypt istnieje i jego przeznaczenie jest jasne, uruchom go automatycznie.

Jeśli nie ma takiego skryptu, samodzielnie zaktualizuj oczywisty plik źródłowy z wersją.

Aktualizuj tylko pola, które jednoznacznie przechowują ten sam numer wersji projektu. Nie zmieniaj wersji zależności, target frameworków, wersji narzędzi, plików lock ani wygenerowanych artefaktów, chyba że uruchomiony skrypt projektu zrobi to sam.

Jeśli istnieje kilka równorzędnych źródeł wersji i nie wiadomo, które jest kanoniczne, zaraportuj niejednoznaczność i nie propaguj wersji ręcznie. Jeśli nie znajdziesz żadnego miejsca do propagacji, poinformuj użytkownika, że wersja została zaktualizowana tylko w `version.md`.

## Format krytyczny

Traktuj format pliku jako ścisły, nie jako luźną sugestię.

Na tym pliku pracują automatyczne skrypty użytkownika. Układ nagłówków, punktów i pustych linii jest częścią kontraktu technicznego, a nie tylko kwestią czytelności.

Nowy wpis musi wyglądać dokładnie tak:

```md
# x.y.z YYYY-MM-DD HH:mm
- Opis zmiany pierwszej..
- Opis zmiany drugiej...

```

Zasady obowiązkowe:

- nagłówek nowej wersji ma mieć postać dokładnie `# x.y.z YYYY-MM-DD HH:mm`,
- jeśli masz dostęp do bieżącej daty i godziny, wpisz je w nagłówku,
- jeśli nie masz dostępu do bieżącej daty i godziny, zaraportuj ten brak i nie zgaduj wartości,
- między nagłówkiem wersji a pierwszym punktem nie może być pustej linii,
- między kolejnymi punktami nie może być pustych linii,
- po ostatnim punkcie nowej wersji ma być dokładnie jedna pusta linia,
- ta jedna pusta linia ma oddzielać opis bieżącej wersji od poprzedniego wpisu,
- nie dodawaj dodatkowych pustych linii ani nie pomijaj tej jednej pustej linii, bo skrypty używają jej do rozpoznania końca opisu ostatniej wersji.

## Skąd brać informacje

Oceniaj zmiany wyłącznie na podstawie:

- aktualnego stanu plików w projekcie,
- zmian widocznych w workspace,
- faktów ustalonych w bieżącej rozmowie,
- ewentualnie lokalnych dokumentów projektu, jeśli rzeczywiście opisują bieżący stan.

Nie opieraj wpisu na przypuszczeniach.

## Co powinno trafić do `version.md`

Uwzględniaj tylko rzeczy, które opisują realną zmianę w projekcie, na przykład:

- nowe funkcje,
- naprawione błędy,
- zmiany zachowania komend, endpointów, modeli albo UI,
- ważne testy lub walidacje,
- istotne zmiany konfiguracji,
- refaktoryzacje, jeśli wpływają na czytelność, stabilność albo utrzymanie.

## Czego nie wpisywać

Nie wpisuj:

- dygresji z rozmowy,
- szerokich planów na przyszłość,
- decyzji procesowych niezwiązanych bezpośrednio ze zmianą w projekcie,
- zwykłych stanów pośrednich, które nie domykają etapu,
- ogólników,
- szczegółów bez znaczenia dla changeloga.

## Zasada końcowa

`version.md` ma odpowiadać na pytanie: `co zmieniono w projekcie?`

Jeśli jakaś informacja lepiej pasuje do pamięci projektu albo listy dalszych kroków, nie umieszczaj jej tutaj.
