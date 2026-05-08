---
name: inc-version
description: Zwiększ patch version projektu i przygotuj nowy wpis na początku pliku `version.md` w katalogu głównym repozytorium.
info: Komenda wspólna. Zakłada ścisły format `version.md`, bo plik może być przetwarzany przez skrypty użytkownika.
implicit-invocation: false
version: 3
modified-date: 2026-04-03
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

- Pracuj zawsze na pliku `version.md` w katalogu głównym projektu.
- Nie modyfikuj żadnych innych plików niż `version.md`, nawet jeśli w repo istnieją skrypty lub konwencje synchronizujące numer wersji do kodu; taka synchronizacja należy do osobnego, późniejszego kroku użytkownika.
- Jeśli plik nie istnieje, utwórz go pod dokładnie tą nazwą.
- Zmieniaj wyłącznie ostatni segment wersji, czyli patch.
- Nie zmieniaj major ani minor.
- Nowy wpis zawsze wstawiaj na początek pliku.
- Nie modyfikuj wcześniejszych wpisów.
- Nie zgaduj. Jeśli brakuje danych do wpisu, pomiń niepewny punkt i zapytaj użytkownika, czy dana informacja ma trafić do `version.md`.
- Jeśli plik istnieje, najpierw sprawdź czy jego format jest zgodny z przyjętym schematem. Jeśli format jest błędny albo niejednoznaczny, zaraportuj problem i niczego nie modyfikuj.

## Format krytyczny

Traktuj format pliku jako ścisły, nie jako luźną sugestię.

Na tym pliku pracują automatyczne skrypty użytkownika. Układ nagłówków, punktów i pustych linii jest częścią kontraktu technicznego, a nie tylko kwestią czytelności.

Nowy wpis musi wyglądać dokładnie tak:

```md
# x.y.z YYYY-MM-DD HH:mm
- Added validation for missing config files / Prevents silent startup failures
- Fixed version parsing in release script / Keeps patch bump stable for double-digit versions

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

## Format wpisu

Traktuj tę sekcję jako opis treści punktów, a nie jako osobny ścisły szablon znak po znaku.

Stosuj krótki, techniczny format:

- nagłówek `# x.y.z YYYY-MM-DD HH:mm`,
- pod nim 3-6 punktów,
- każdy punkt ma zawierać dokładnie dwie części oddzielone znakiem ` / `:
  opis zmiany / sens zmiany albo efekt
- nie dodawaj dodatkowych etykiet typu `WHAT`, `WHY`, `STATUS` ani innych znaczników pomocniczych.

## Styl

- Pisz konkretnie i rzeczowo.
- Zamiast ogólników napisz, co faktycznie zmieniono.
- Opisuj efekt dla użytkownika, stabilności albo dalszej pracy.
- Jeśli masz wątpliwość, pomiń punkt zamiast dopisywać ogólnik albo domysł.
- Unikaj duplikatów między punktami.
- Nie zaciemniaj wpisu dodatkowymi etykietami ani komentarzami spoza ustalonego formatu.

## Zasada końcowa

`version.md` ma odpowiadać na pytanie: `co zmieniono w projekcie?`

Jeśli jakaś informacja lepiej pasuje do pamięci projektu albo listy dalszych kroków, nie umieszczaj jej tutaj.
