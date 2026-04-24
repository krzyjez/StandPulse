# StandPulse — notatki projektowe MVP

## 1. Cel projektu

StandPulse ma pilnować, żebym nie siedział zbyt długo bez przerwy przy biurku. 
System ma wykrywać ciągłą obecność w strefie biurka, liczyć czas, a po przekroczeniu limitu pokazywać wyraźny komunikat na małym monitorze.

Reset cyklu ma następować dopiero wtedy, gdy użytkownik rzeczywiście odejdzie od biurka na minimalny, skonfigurowany czas.

## 2. Główne założenia

1. Logika działa na komputerze z Windows.
2. Czujnik obecności jest obowiązkowy — bez niego projekt nie ma sensu.
3. Nie tworzymy osobnego „mózgu” urządzenia, jeśli komputer może pełnić tę rolę.
4. Ostrzeżenie powinno być widoczne na osobnym małym monitorze, a nie tylko w głównym oknie aplikacji.
5. Reset nie następuje po chwilowym poderwaniu się, tylko po realnej przerwie poza biurkiem.

## 3. Proponowana logika działania

### Stan podstawowy
- użytkownik jest wykrywany w strefie biurka,
- aplikacja liczy czas ciągłej obecności.

### Po przekroczeniu limitu
- aplikacja aktualizuje widok na małym monitorze,
- widok może mieć różne tryby, np.:
  - spokojny widok = OK,
  - wyraźne ostrzeżenie = przekroczony limit,
  - animowany / pulsujący ekran = alarm aktywny.

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
3. po przekroczeniu limitu aktualizować widok ostrzegawczy na małym monitorze,
4. liczyć czas nieobecności po odejściu od biurka,
5. resetować cykl dopiero po osiągnięciu minimalnego czasu przerwy.

## 11. Proponowane stany aplikacji

Minimalny model stanów:

1. `PresentOk` — użytkownik obecny, limit jeszcze nieprzekroczony,
2. `Warning` — użytkownik obecny, limit przekroczony, widok ostrzegawczy aktywny,
3. `BreakCounting` — użytkownik opuścił strefę, trwa odliczanie minimalnej przerwy,
4. `Reset` — przerwa zaliczona, licznik rozpoczyna nowy cykl.

## 12. Parametry konfiguracyjne MVP

Warto od razu przewidzieć konfigurowalne ustawienia:

1. `WorkIntervalMinutes` — ile minut ciągłej obecności uruchamia alarm,
2. `BreakResetSeconds` — ile sekund nieobecności resetuje cykl,
3. `PresenceDebounceMs` — filtracja krótkich skoków sygnału,
4. `DisplayModeWarning` — jaki widok odpowiada alarmowi,
5. `DisplayModeOk` — jaki widok oznacza stan normalny.

## 13. Mały monitor — założenia

Aktualny kierunek sygnalizacji to mały monitor sterowany z aplikacji Windows.
Monitor ma działać jako osobny, zawsze widoczny ekran statusu StandPulse.

Wymagania wstępne:

1. powinien dobrze wyglądać przy głównym stanowisku pracy,
2. powinien dać się łatwo postawić obok monitora albo pod monitorem,
3. powinien być sterowany bezpośrednio przez aplikację Windows,
4. powinien pokazywać stan, czas obecności i ostrzeżenia,
5. może pokazywać diagnostykę sensora, np. odległość oraz motion/static energy.

Najprostszy model techniczny:

1. mały monitor działa jako dodatkowy ekran w Windows,
2. aplikacja otwiera na nim dedykowane okno statusu,
3. okno może działać w trybie pełnoekranowym albo kioskowym,
4. aplikacja nie musi integrować się z zewnętrznym API urządzenia.

## 14. Najważniejsze pytania na start prac

1. Czy zestaw XIAO + mmWave będzie od razu wygodnie widoczny w Windows jako port COM?
2. Jakie dane dokładnie dostaniemy z sensora: obecność, mikroruch, odległość, status strefy?
3. Jaki mały monitor najlepiej sprawdzi się jako osobny ekran statusu?
4. Czy do pierwszej wersji wystarczy prosta interpretacja: „obecny / nieobecny”?

## 15. Proponowany pierwszy etap prac

1. Kupić i uruchomić zestaw XIAO + 24GHz mmWave for XIAO.
2. Sprawdzić zachowanie w Windows po podłączeniu USB-C.
3. Napisać prosty program testowy w C#, który tylko loguje zmiany stanu obecności.
4. Zmierzyć, czy sensor sensownie rozpoznaje odejście od biurka i powrót.
5. Dopiero potem dobrać finalny mały monitor i sposób wyświetlania statusu.

## 16. Podsumowanie robocze

Najbardziej sensowna koncepcja MVP StandPulse na dziś:

- **czujnik:** Seeed Studio 24GHz mmWave for XIAO,
- **połączenie z PC:** przez USB-C na płytce XIAO,
- **logika:** aplikacja Windows w C#,
- **pomiar:** ciągła obecność w strefie biurka,
- **reset:** dopiero po minimalnym czasie nieobecności,
- **sygnalizacja:** osobny mały monitor sterowany przez aplikację Windows.

## 17. Ustalenia robocze: sygnały z XIAO

Nie należy traktować etykiety `StaticTarget` jako pewnej informacji, że użytkownik „siedzi statycznie”.
Granice między `MovingTarget`, `StaticTarget` i `BothTargets` zależą od algorytmu radaru, odległości, czułości i realnych mikroruchów człowieka.
Przykładowo osoba siedząca 50 cm od czujnika i poruszająca ręką albo twarzą może być klasyfikowana różnie w kolejnych próbkach.

Dla StandPulse ważniejsze są:

1. **odległość od czujnika** — sygnał pierwszej klasy, potrzebny do określenia strefy biurka,
2. **ciągłość obecności w zadanym zakresie odległości**,
3. **motion energy** i **static energy** — szczególnie przydatne do diagnostyki, kalibracji i wizualizacji,
4. **status celu** (`NoTarget`, `MovingTarget`, `StaticTarget`, `BothTargets`) — pomocnicza etykieta, a nie główna semantyka aplikacji.

Docelowo logika obecności powinna być bliższa temu modelowi:

```text
present = targetDetected && distanceCm >= DeskMinDistanceCm && distanceCm <= DeskMaxDistanceCm
```

gdzie:

- `NoTarget` oznacza brak celu,
- `MovingTarget`, `StaticTarget` i `BothTargets` oznaczają wykryty cel,
- zakres odległości określa, czy wykryty cel znajduje się w strefie biurka.

## 18. Tryb danych: podstawowy i diagnostyczny

W trybie podstawowym XIAO powinno wysyłać do aplikacji Windows uproszczone próbki, np.:

```json
{"present":true,"targetStatus":"BothTargets","distanceCm":62,"motionEnergy":48,"staticEnergy":31}
{"present":false,"targetStatus":"NoTarget","distanceCm":null,"motionEnergy":0,"staticEnergy":0}
```

W trybie diagnostycznym / inżynieryjnym warto rozważyć przesyłanie energii per bramka odległości:

```json
{
  "present": true,
  "distanceCm": 62,
  "motionGates": [0, 3, 18, 44, 12, 2, 0, 0, 0],
  "staticGates": [0, 0, 9, 35, 28, 4, 0, 0, 0]
}
```

Taki strumień może służyć do wizualizacji, np. prostego histogramu energii w kolejnych zakresach odległości.
To może być bardzo przydatne przy ustawianiu czujnika na biurku i dobieraniu progów, ale nie musi być wymagane w minimalnej logice MVP.

## 19. Komunikacja z aplikacją Windows

Aplikacja Windows powinna nasłuchiwać portu COM udostępnianego przez XIAO po USB.

Docelowy przepływ danych:

```text
mmWave sensor -> UART -> XIAO -> USB serial / COM -> aplikacja Windows
```

XIAO pełni rolę adaptera:

1. czyta surowe ramki z radaru po UART,
2. parsuje je lokalnie,
3. wysyła do komputera prostszy strumień danych,
4. ukrywa przed aplikacją C# szczegóły binarnego protokołu radaru.

Nie zakładamy, że XIAO wysyła dane tylko przy zmianie stanu.
Lepszy model dla StandPulse to ciągły strumień próbek, np. co 100-250 ms.
Dzięki temu aplikacja może:

1. filtrować krótkie skoki sygnału,
2. liczyć stabilną obecność i nieobecność,
3. reagować na zmianę odległości,
4. rysować diagnostykę energii w czasie,
5. podejmować decyzje na podstawie kilku ostatnich próbek, a nie pojedynczego odczytu.

Proponowany kierunek MVP:

1. XIAO wysyła próbkę co około 200 ms.
2. Aplikacja C# stale nasłuchuje portu COM.
3. Dane z XIAO są przesyłane jako JSON Lines albo inny prosty format tekstowy.
4. Decyzja `present` / `absent` jest filtrowana po stronie aplikacji.
5. Tryb z `motionGates` i `staticGates` traktujemy jako opcjonalny tryb diagnostyczny.

---

## Linki robocze

- Seeed Studio — mmWave for XIAO:
  - https://wiki.seeedstudio.com/mmwave_for_xiao/
