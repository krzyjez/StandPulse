# Specyfikacja sterowania Shelly Plug S

## Cel

`StandPulse.SensorHost` odpowiada za automatyczne sterowanie gniazdkiem Shelly Plug S na podstawie roboczej obecnosci uzytkownika przy stanowisku.

Robocza obecnosc laczy dwa warunki:

1. sensor StandPulse wykrywa stabilna obecnosc przy biurku,
2. sesja Windows jest aktywna dla pracy uzytkownika.

Gniazdko jest wlaczone tylko wtedy, gdy oba warunki sa spelnione. W pozostalych stanach system dazy do wylaczenia gniazdka.

## Wlasciciel logiki

Sterowanie Shelly jest czescia `StandPulse.SensorHost`, a nie osobna aplikacja i nie `StandPulse.ScannerViewer`.

`StandPulse.SensorHost` jest produkcyjnym wlascicielem sensora obecnosci, dziala stale z autostartu uzytkownika i centralizuje decyzje o stanie stanowiska. `StandPulse.ScannerViewer` pozostaje narzedziem diagnostycznym oraz widokiem stanu, ale nie wykonuje automatyki zasilania.

## Przeplyw decyzyjny

```text
sensor mmWave -> StandPulse.SensorHost -> stable sensor status
Windows session -> StandPulse.SensorHost -> session status
stable sensor status + session status -> work presence
work presence -> Shelly Plug S
```

`work presence` przyjmuje wartosc `true`, gdy:

1. status sensora to `presence`,
2. sesja Windows jest zalogowana i odblokowana.

`work presence` przyjmuje wartosc `false`, gdy:

1. status sensora to `absence`,
2. status sensora to `sensorNotAvailable`,
3. status sensora to `error`,
4. sesja Windows jest zablokowana,
5. sesja Windows przechodzi w wylogowanie albo zamkniecie.

## Zachowanie gniazdka

Shelly Plug S otrzymuje polecenie `ON`, gdy `work presence = true`.

Shelly Plug S otrzymuje polecenie `OFF`, gdy `work presence = false`.

Wylaczenie jest bezpiecznym stanem domyslnym. Blad sensora, brak probek, blad odczytu statusu sesji albo konczenie sesji Windows prowadzi do proby wylaczenia gniazdka.

## Stabilizacja i opoznienia

Status sensora korzysta z istniejacej stabilizacji `presence.stateChangeDelaySeconds`.

Dla wylaczenia zasilania system uzywa dodatkowego opoznienia, niezaleznego od stabilizacji sensora. Opoznienie chroni przed chwilowymi zanikami obecnosci, krotkim zablokowaniem sesji albo przejsciowym brakiem probek.

Dla zdarzen konczenia sesji Windows opoznienie nie jest stosowane. `StandPulse.SensorHost` od razu probuje wylaczyc Shelly przed zakonczeniem procesu.

## Stan sesji Windows

`StandPulse.SensorHost` rozroznia co najmniej te stany sesji:

1. `active` - uzytkownik jest zalogowany i sesja jest odblokowana,
2. `locked` - sesja jest zablokowana,
3. `ending` - trwa wylogowanie, zamykanie systemu albo konczenie sesji,
4. `unknown` - status sesji nie jest dostepny.

Tylko `active` pozwala wlaczyc Shelly. Pozostale stany wymuszaja `work presence = false`.

## Tryb reczny

System obsluguje reczne nadpisanie decyzji automatyki.

Tryb reczny ma trzy wartosci:

1. `auto` - Shelly jest sterowany z `work presence`,
2. `forceOn` - Shelly pozostaje wlaczony niezaleznie od `work presence`,
3. `forceOff` - Shelly pozostaje wylaczony niezaleznie od `work presence`.

Tryb reczny jest jawnie widoczny w statusie publikowanym przez hosta, zeby viewer nie pokazywal automatycznej decyzji jako decyzji faktycznie wykonanej.

## Publikowany status

`StandPulse.SensorHost` publikuje stan potrzebny aplikacjom klienckim:

1. stabilny status sensora,
2. status sesji Windows,
3. wyliczone `work presence`,
4. tryb reczny,
5. oczekiwany stan Shelly,
6. ostatni znany stan Shelly,
7. czas ostatniej udanej komendy,
8. ostatni blad komunikacji z Shelly.

Viewer korzysta z tego statusu do prezentacji i ewentualnego ustawienia trybu recznego.

## Komunikacja z Shelly

Warstwa komunikacji z Shelly jest odseparowana od logiki decyzji. Logika decyzji przekazuje tylko oczekiwany stan `on/off`.

Konfiguracja zawiera co najmniej:

1. wlaczenie lub wylaczenie integracji Shelly,
2. adres urzadzenia,
3. timeout komendy,
4. liczbe ponowien,
5. opoznienie wylaczenia,
6. tryb reczny po starcie procesu.

## Odpornosc na bledy

Nieudana komenda do Shelly jest logowana i ponawiana zgodnie z konfiguracja.

Host nie blokuje odczytu sensora ani serwera named pipe podczas komunikacji z Shelly.

Po odzyskaniu komunikacji host porownuje oczekiwany stan z ostatnim znanym stanem Shelly i wysyla brakujaca komende, jezeli stan urzadzenia rozni sie od decyzji systemu.

## Zakres poza dokumentem

Ten dokument nie wybiera ostatecznego protokolu komunikacji z Shelly. Dopuszczalne sa lokalne API Shelly, Matter albo inna lokalna integracja, o ile nie wymaga dzialania osobnej aplikacji sterujacej poza `StandPulse.SensorHost`.
