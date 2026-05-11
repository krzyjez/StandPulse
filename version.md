# 0.1.1 2026-05-11 13:58
- Dodano limit `presence.maxStartupInvalidLines` w SensorHost / Dlugotrwale smieci startowe sensora przechodza w publiczny status `error`
- Rozszerzono obsluge niepoprawnych linii przed pierwsza probka / Krotki szum startowy nadal jest ignorowany, ale nie ukrywa trwalej awarii
- Dodano scenariusz fake `startup-garbage-becomes-error` / Eskalacja smieci startowych jest sprawdzana automatycznie bez fizycznego sensora
- Zaktualizowano dokumentacje i paczke `bin/StandPulse.SensorHost` / Opublikowany host i opis konfiguracji odpowiadaja nowemu zachowaniu
- Przelaczono autostart SensorHost na `run` / Codzienne uruchomienie nie generuje juz logu debug

# 0.1.0 2026-05-08 09:24
- Dodano system pracy `ai` dla repozytorium / StandPulse ma lokalne reguły, skille Codexa i pliki operacyjne do dalszej pracy z agentem
