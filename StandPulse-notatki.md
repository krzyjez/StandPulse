# StandPulse — notatki projektowe MVP

## 1. Cel projektu

StandPulse ma pilnować, żebym nie siedział zbyt długo bez przerwy przy biurku. 
System ma wykrywać ciągłą obecność w strefie biurka, liczyć czas, a po przekroczeniu limitu uruchamiać zewnętrzny sygnał świetlny.

Reset cyklu ma następować dopiero wtedy, gdy użytkownik rzeczywiście odejdzie od biurka na minimalny, skonfigurowany czas.

## 2. Główne założenia

1. Logika działa na komputerze z Windows.
2. Czujnik obecności jest obowiązkowy — bez niego projekt nie ma sensu.
3. Nie tworzymy osobnego „mózgu” urządzenia, jeśli komputer może pełnić tę rolę.
4. Sygnał ostrzegawczy powinien być fizyczny, a nie tylko ekranowy.
5. Reset nie następuje po chwilowym poderwaniu się, tylko po realnej przerwie poza biurkiem.

## 3. Proponowana logika działania

### Stan podstawowy
- użytkownik jest wykrywany w strefie biurka,
- aplikacja liczy czas ciągłej obecności.

### Po przekroczeniu limitu
- aplikacja uruchamia sygnał świetlny,
- sygnał może mieć różne tryby, np.:
  - zielony = OK,
  - czerwony = przekroczony limit,
  - czerwony migający = alarm aktywny.

### Reset cyklu
- aplikacja wykrywa brak obecności w strefie biurka,
- rozpoczyna licznik przerwy,
- jeśli brak obecności trwa nieprzerwanie co najmniej przez zadany czas (np. 60 sekund), cykl zostaje zresetowany,
- jeśli użytkownik wróci wcześniej, alarm pozostaje aktywny albo wraca natychmiast.

## 4. Ważne uproszczenie modelu

System nie musi wykrywać dosłownie pozycji „siedzę” kontra „stoję”.
W praktyce wystarczy mierzyć:

**ciągłą obecność w strefie biurka**

To daje prawie ten sam efekt zdrowotny, a jest znacznie prostsze i bardziej realistyczne technicznie.

## 5. Czujnik mmWave — dlaczego właśnie ten

Do StandPulse najlepiej pasuje czujnik typu mmWave, bo:

1. wykrywa obecność nawet przy bardzo małych ruchach,
2. jest lepszy od PIR do scenariusza „człowiek siedzi przy biurku”,
3. nadaje się do mierzenia, czy użytkownik nadal znajduje się w strefie pracy,
4. pozwala wiarygodniej odróżnić „jestem przy biurku” od „odszedłem od biurka”.

PIR byłby za słaby do tego zastosowania, bo dobrze wykrywa ruch, ale słabiej obecność osoby siedzącej prawie nieruchomo.

## 6. Wybrany kierunek: Seeed Studio 24GHz mmWave for XIAO

Najbardziej obiecujący wariant do MVP to zestaw oparty o:

1. **Seeed Studio XIAO**
2. **24GHz mmWave for XIAO**

### Kluczowe zrozumienie architektury

To jest najważniejszy punkt roboczy:

**USB-C jest na płytce XIAO, nie na samym radarze jako „gotowym sensorku USB”.**

Oznacza to, że:

1. zestaw ze zdjęcia może być podłączony do komputera przez USB-C,
2. sam radar nie jest klasycznym urządzeniem USB,
3. radar komunikuje się z płytką XIAO przez GPIO/UART,
4. dopiero XIAO jest podłączone do PC przez USB-C.

Czyli architektura wygląda tak:

`mmWave radar -> UART/GPIO -> XIAO -> USB-C -> komputer z Windows`

To podejście bardzo dobrze pasuje do MVP, bo z punktu widzenia komputera całość może wyglądać jak urządzenie podłączone przewodem USB-C.

## 7. Co wynika z dokumentacji Seeed

Na podstawie strony Seeed Studio o „mmWave for XIAO”:

1. jest to **expansion board for XIAO**,
2. sensor oparty jest o **24 GHz FMCW**,
3. jest przeznaczony do wykrywania osób w ruchu i w stanie względnej statyczności,
4. po poprawnym złożeniu zestawu można podłączyć go do komputera lub zasilania przez USB-C,
5. komunikacja po stronie sensora opiera się na interfejsach GPIO/UART.

Źródło:
- https://wiki.seeedstudio.com/mmwave_for_xiao/

## 8. Dlaczego ten wariant jest dobry na start

1. jest mały i schludny,
2. nie wygląda jak plątanina luźnych przewodów,
3. ma USB-C po stronie płytki sterującej,
4. jest bliższy „gotowemu zestawowi” niż surowy moduł radarowy,
5. dobrze nadaje się do testów z aplikacją na Windows.

## 9. Otwarta kwestia do potwierdzenia praktycznego

Należy sprawdzić w praktyce:

**czy po podłączeniu zestawu XIAO + mmWave do Windows urządzenie pojawia się jako port COM wygodny do użycia z aplikacji w C#**

To jest bardzo prawdopodobny i sensowny kierunek, ale trzeba to potwierdzić podczas pierwszych testów.

## 10. Docelowa rola aplikacji Windows

Aplikacja na PC powinna:

1. odczytywać stan obecności z czujnika,
2. liczyć czas ciągłej obecności w strefie biurka,
3. po przekroczeniu limitu aktywować sygnał świetlny,
4. liczyć czas nieobecności po odejściu od biurka,
5. resetować cykl dopiero po osiągnięciu minimalnego czasu przerwy.

## 11. Proponowane stany aplikacji

Minimalny model stanów:

1. `PresentOk` — użytkownik obecny, limit jeszcze nieprzekroczony,
2. `Warning` — użytkownik obecny, limit przekroczony, sygnał ostrzegawczy aktywny,
3. `BreakCounting` — użytkownik opuścił strefę, trwa odliczanie minimalnej przerwy,
4. `Reset` — przerwa zaliczona, licznik rozpoczyna nowy cykl.

## 12. Parametry konfiguracyjne MVP

Warto od razu przewidzieć konfigurowalne ustawienia:

1. `WorkIntervalMinutes` — ile minut ciągłej obecności uruchamia alarm,
2. `BreakResetSeconds` — ile sekund nieobecności resetuje cykl,
3. `PresenceDebounceMs` — filtracja krótkich skoków sygnału,
4. `LedModeWarning` — jaki sygnał świetlny odpowiada alarmowi,
5. `LedModeOk` — jaki sygnał oznacza stan normalny.

## 13. Sygnał świetlny — założenia

Sygnał świetlny powinien być czymś gotowym lub prawie gotowym, a nie surową taśmą LED do samodzielnego montażu w obudowie.

Wymagania wstępne:

1. powinien dobrze wyglądać przy monitorze,
2. najlepiej żeby dało się go po prostu postawić lub założyć na monitor,
3. powinien umożliwiać sterowanie z komputera,
4. idealnie przez lokalne API po Wi‑Fi.

Na tym etapie warto szukać raczej:

1. light barów stawianych obok monitorów,
2. monitor light barów zakładanych na górę monitora,
3. małych lampek RGB z lokalnym sterowaniem.

## 14. Kandydaci do dalszego researchu dla części świetlnej

Do dalszego sprawdzenia:

1. **Govee Light Bars / Gaming Light Bars**
   - plus: estetyczne, gotowe,
   - plus: część urządzeń Govee wspiera LAN API,
   - minus: trzeba sprawdzić wsparcie dla konkretnego modelu.

2. **WiZ Light Bars / lampki WiZ**
   - plus: znane lokalne sterowanie w ekosystemie WiZ,
   - minus: trzeba sprawdzić, które konkretne modele najlepiej pasują wizualnie do monitora.

3. **monitor light bary z podświetleniem ambient**
   - plus: mogą ładnie nakładać się na monitor,
   - minus: nie każdy model ma sensowne API.

## 15. Najważniejsze pytania na start prac

1. Czy zestaw XIAO + mmWave będzie od razu wygodnie widoczny w Windows jako port COM?
2. Jakie dane dokładnie dostaniemy z sensora: obecność, mikroruch, odległość, status strefy?
3. Jaki element świetlny będzie najlepiej wyglądał przy monitorze i jednocześnie dawał się sterować lokalnie?
4. Czy do pierwszej wersji wystarczy prosta interpretacja: „obecny / nieobecny”?

## 16. Proponowany pierwszy etap prac

1. Kupić i uruchomić zestaw XIAO + 24GHz mmWave for XIAO.
2. Sprawdzić zachowanie w Windows po podłączeniu USB-C.
3. Napisać prosty program testowy w C#, który tylko loguje zmiany stanu obecności.
4. Zmierzyć, czy sensor sensownie rozpoznaje odejście od biurka i powrót.
5. Dopiero potem dobrać finalny element świetlny.

## 17. Podsumowanie robocze

Najbardziej sensowna koncepcja MVP StandPulse na dziś:

- **czujnik:** Seeed Studio 24GHz mmWave for XIAO,
- **połączenie z PC:** przez USB-C na płytce XIAO,
- **logika:** aplikacja Windows w C#,
- **pomiar:** ciągła obecność w strefie biurka,
- **reset:** dopiero po minimalnym czasie nieobecności,
- **sygnalizacja:** zewnętrzny element świetlny sterowany z komputera.

---

## Linki robocze

- Seeed Studio — mmWave for XIAO:
  - https://wiki.seeedstudio.com/mmwave_for_xiao/

- Do dalszego sprawdzenia pod kątem oświetlenia:
  - Govee LAN API:
    - https://app-h5.govee.com/user-manual/wlan-guide
  - WiZ local / developer ecosystem:
    - https://docs.pro.wizconnected.com/
    - https://gitlab.com/wizlighting/wiz-local-control
