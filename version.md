# 0.1.3 2026-06-08 14:00
- Dodano `workPresenceSince` do snapshotu SensorHost / Klienci moga odroznic czas obecnosci roboczej od stabilizacji fizycznego sensora
- Rozszerzono log SensorHost o pelne znaczniki czasu / Zmiany statusu sa diagnozowalne bez dopasowywania samej godziny

# 0.1.2 2026-05-14 11:39
- Rozszerzono log stabilnych zmian SensorHost o dane ostatniej probki / Konsola pokazuje przyczyne przejscia w `presence` albo `absence` bez zalewania probkami
- Dodano odczyt wersji projektu z `version.md` podczas kompilacji / Assembly version aplikacji odpowiada najnowszemu wpisowi changeloga
- Pokazano wersje w naglowku SensorHost i w oknie ScannerViewer / Latwiej sprawdzic, jaki build jest aktualnie uruchomiony
- Sprawdzono build SensorHost i fake scenariusze sensora / Zmiana logowania zachowuje dotychczasowa stabilizacje statusu

# 0.1.1 2026-05-11 13:58
- Dodano limit `presence.maxStartupInvalidLines` w SensorHost / Dlugotrwale smieci startowe sensora przechodza w publiczny status `error`
- Rozszerzono obsluge niepoprawnych linii przed pierwsza probka / Krotki szum startowy nadal jest ignorowany, ale nie ukrywa trwalej awarii
- Dodano scenariusz fake `startup-garbage-becomes-error` / Eskalacja smieci startowych jest sprawdzana automatycznie bez fizycznego sensora
- Zaktualizowano dokumentacje i paczke `bin/StandPulse.SensorHost` / Opublikowany host i opis konfiguracji odpowiadaja nowemu zachowaniu
- Przelaczono autostart SensorHost na `run` / Codzienne uruchomienie nie generuje juz logu debug

# 0.1.0 2026-05-08 09:24
- Dodano system pracy `ai` dla repozytorium / StandPulse ma lokalne reguły, skille Codexa i pliki operacyjne do dalszej pracy z agentem
