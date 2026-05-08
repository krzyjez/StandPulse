# Project Memory

## Sources Of Truth

- `MyRules Docs/project-desc-for-ai.md` to podstawowy skrót domeny, architektury i invariants dla pracy AI.
- `MyRules Docs/project-description.md` zawiera pełny opis projektu; przy konflikcie interpretacyjnym jest źródłem prawdy przed starszymi notatkami roboczymi.
- Sekcja `Error Handling Strategy` w `MyRules Docs/project-desc-for-ai.md` i `MyRules Docs/project-description.md` opisuje docelowy model obsługi błędów; nie upraszczać go do jednego wzorca.
- Dla zamkniętych tygodni źródłem danych historycznych jest snapshot `weekOutcome.rulesBag`, nie bieżący globalny `rulesBag`.

## Architecture

- Projekt składa się z frontendu Flutter + Riverpod oraz backendu Azure Functions (.NET, Azure Table Storage).
- GoRouter musi być singletonem utrzymywanym w `routerProvider`; nie wolno tworzyć go ponownie w `build()`.
- Provider tworzący router musi obserwować `authProvider`, aby przekierowania reagowały na zmianę stanu autentykacji.
- Week Movement jest oparte o `selectedWeekStartProvider` (`null` = bieżący tydzień), a `weekProvider` pozostaje singletonem reagującym na wybrany tydzień.
- Dla błędów zapisu tygodnia istnieje osobny przepływ przez `WeekSaveErrorProvider`; błąd save nie powinien być traktowany jak błąd load.
- `AzureFunctions.DataSeeder` jest lokalnym narzędziem CLI do zasilania Azurite danymi testowymi; komenda zbiorcza nazywa się `rules-and-weeks`.

## Domain Rules

- `RuleAction.Order` używa indeksowania 0-based; walidacja ma pilnować unikalności `Order` w obrębie reguły, a nie wymuszać zakres `> 0`.
- Zamknięty tydzień jest tylko do odczytu: UI blokuje edycję, a provider ma dodatkową ochronę logiczną przed zapisem.
- Brak snapshotu `RulesBag` w zamkniętym tygodniu to błąd danych i powinien kończyć się fail-fast (`StateError`), bez fallbacku do aktualnych danych.
- Po zmianie reguł backend musi przeliczyć wyniki tygodnia; frontend po zapisie reguły powinien odświeżyć dane tygodnia.
- `Options` użytkownika są częścią danych użytkownika zwracanych po logowaniu; ustawienia zapisują się przez `PUT /me`, a lokalny storage służy głównie do sesji/tokenu.
- `PasswordHash` nie może być ukrywany przez `[JsonIgnore]`, bo ten sam model jest używany także do storage; dane wychodzące przez HTTP muszą być oczyszczane jawnie przez `WithoutPassword()`.
- `UpdateUser` nie może pozwalać frontendowi na zmianę `Login` ani `PasswordHash`.
- Backendowy format ikony to obiekt `{CodePoint, FontFamily, FontPackage}`; frontend ma używać tego samego kontraktu.

## Operational Conventions

- Repo jest prowadzone w środowisku Windows + PowerShell.
- Skrypty `.ps1` warto pisać ASCII-only albo zapisywać jawnie jako UTF-8; błędne kodowanie polskich znaków potrafi rozsypać parser PowerShell i dawać mylące błędy o niedomkniętych stringach.
- `project-memory.md` przechowuje tylko trwałe i półtrwałe ustalenia; nie jest changelogiem ani listą TODO.
- `version.md` pozostaje plikiem append-only z nowymi wpisami dodawanymi na początku.
- Po zmianach w providerach Riverpod zwykle trzeba uruchomić code generation (`build_runner`).
- W taskach wymagających loginu w PowerShell lepiej używać `Read-Host` niż globalnych `inputs` VS Code.
- Testy jednostkowe walidacji mogą działać bez Azurite; testy HTTP/integracyjne powinny same jawnie weryfikować infrastrukturę.

## Pitfalls

- Nie invalidować providerów w reakcji na błąd API, w interceptorach ani callbackach dialogów błędów; to prowadzi do pętli requestów.
- `logout()` nie powinien masowo invalidować providerów; przekierowanie ma wynikać ze zmiany auth state, a czyszczenie danych z kolejnego logowania lub jawnego refreshu.
- Nie tworzyć convenience providerów ukrywających błędy przez fallback do `[]`; fail-fast jest celowy.
- Dopuszczalny fallback do `null` w providerach pomocniczych wymaga jawnego logowania błędu i stack trace.
- W projekcie obowiązują trzy różne wzorce error handlingu: SnackBar dla autosave/tła, Dialog dla błędów ładowania, SaveResult dla akcji użytkownika.
- Riverpod potrafi ukryć stack trace, jeśli błąd nie zostanie zalogowany przed przechwyceniem.
- Azure Functions w tym projekcie używa Newtonsoft.Json w miejscach, gdzie łatwo założyć System.Text.Json; przed pisaniem converterów trzeba sprawdzić realny serializer.
- Newtonsoft.Json nie obsługuje bezpiecznie scenariusza z C# `required` w modelach zapisywanych do Azure Table Storage.
- `[SetUpFixture]` w NUnit działa na poziomie assembly/namespace, nie jako sensowna klasa bazowa dla testów.
- Pliki z CRLF/BOM potrafią psuć automatyczne edycje; przy podejrzanym braku zmian trzeba sprawdzić endingi i BOM.
- Build backendu może być blokowany przez runtime Azure Functions trzymający `bin/obj`.
