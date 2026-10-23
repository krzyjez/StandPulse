# 0.1.7 2026-10-21 16:38
- `targetDistanceCm` to najmniejsza odleglosc zgloszonych celow i detection distance, wygladzona mediana z 5 probek / Odczyt osoby przy biurku nie skacze juz do 200-300 cm przez dalekie odbicia

# 0.1.6 2026-10-19 23:41
- Firmware parsuje ramki radaru samodzielnie i wysyla odleglosci oraz energie celu ruchomego i nieruchomego / Widac, ze dotychczasowe `distanceCm` pokazuje tlo (sciana ~2,7 m), a nie osobe
- Decyzja obecnosci SensorHost uzywa `targetDistanceCm` wybieranego wedlug statusu celu / Odchylenie na krzesle nie powinno juz dawac odczytu tla ponad progiem
- Dodano komende pipe `samples` z kazda probka i zmianami statusu / Wizualizer moze pokazac biezacy odczyt czujnika z opoznieniem ponizej 1 s
- Klient C# dostal `WatchSamplesAsync`, a SensorProbe komende `samples` / Pomiary odleglosci da sie robic na zywo bez czytania logu

# 0.1.5 2026-10-19 20:26
- Ograniczono log trwalego `radar_read_error` do podsumowania co minute i wpisu o powrocie probek / Wielogodzinna awaria radaru nie wypiera historii z rotowanego logu
- Zapis logu SensorHost nie rzuca juz wyjatkow do wywolujacego / Pelny dysk albo zablokowany plik przy rotacji nie zatrzymuje odczytu sensora ani procesu

# 0.1.4 2026-10-19 19:52
- Timeout SensorHost liczony od ostatniej wiadomosci firmware / Trwaly blad radaru zostaje `error` zamiast przeskakiwac co sekunde na `sensorNotAvailable`
- Uszkodzony `sensor-settings.json` przerywa start z czytelnym bledem / Host nie pracuje po cichu na domyslnym progu, a poprawione wartosci trafiaja do logu jako ostrzezenia
- Dodano nadzorce zadan, async pipe, blokade drugiej instancji i rotacje logu po 5 MB / Ctrl+C konczy host czysto, a nieoczekiwany wyjatek nie wylacza funkcji bez sladu
- Shelly ponawia nieudane komendy z backoffem do 5 min i rozpoznaje `isok:false` / Awaria chmury nie zalewa logu ani API
- Naprawiono wyscig `watch`, odswiezanie `lastSampleAt` i obsluge zlego typu pola w probce / Klient nie gubi zmiany przy subskrypcji, a bledna linia nie zatrzymuje odczytu
- Klient C# i SensorProbe dostaly `workPresenceSince`, timeout polaczenia i kontrole typu odpowiedzi / Kopiowalny klient nie wisi bez hosta
- Skrypty publikacji buduja w katalogu tymczasowym, a scenariusze fake sa odciete od Shelly i rozszerzone do 10 / Nieudany build nie rozbiera paczki w `bin` i nie daje falszywego OK

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
