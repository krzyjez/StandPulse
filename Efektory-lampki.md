# StandPulse – sygnalizatory świetlne i Yeelight Cube

## Cel

Potrzebuję **5–6 niezależnych sygnalizatorów RGB**, sterowanych z komputera przez API.

Każdy sygnalizator ma reprezentować **osobny proces** i pokazywać jego stan za pomocą koloru lub prostego wzoru. Dodatkowo na to ma się nakładać sygnał globalny, np. alarm zasiedzenia się przy biurku – wtedy wszystkie sygnalizatory zaczynają migać.

## Wymagania

* 5–6 niezależnych kanałów świetlnych
* sterowanie z PC, najlepiej lokalnie
* estetyczna forma, bez surowych diod i przypadkowych kabelków
* najlepiej gotowy produkt, nie taśma LED do przyklejania
* dobrze, jeśli moduły można łączyć

## Wniosek na teraz

Najbardziej obiecującym kandydatem jest **Yeelight Cube / Matrix**.

Powody:

1. wygląda estetycznie i biurkowo
2. jest modułowy
3. pojedynczy moduł może potencjalnie pełnić rolę jednego kanału
4. w materiałach producenta widać, że da się wyświetlać litery, liczby, emoji i wzory
5. to wygląda bardziej jak mały „status display” niż zwykła lampka

## Hipoteza robocza

Najbardziej prawdopodobny scenariusz jest taki:

* **1 moduł = 1 kanał / 1 proces**
* kilka modułów połączonych razem daje 5–6 niezależnych sygnalizatorów
* stany procesów mogą być pokazywane kolorem albo prostym wzorem
* sygnał globalny (np. alarm od zasiedzenia) może zmienić zachowanie wszystkich naraz, np. miganie

## Co udało się ustalić

### 1. To wygląda na rodzinę urządzeń, która wspiera bardziej granularne sterowanie niż zwykła lampka

W społeczności Yeelight pojawiają się wzmianki o metodach takich jak:

* `set_segment_rgb`
* `update_leds`

To sugeruje, że sterowanie może dotyczyć nie tylko całego urządzenia jako całości, ale również segmentów albo diod.

### 2. System jest modułowy

Z dokumentacji i opisów wynika, że moduły można łączyć. Przewija się informacja o pracy z maksymalnie 6 modułami w ramach jednego zestawu/bazy.

To bardzo dobrze pasuje do założenia 5–6 procesów.

### 3. Nadal nie ma pełnego potwierdzenia

Na ten moment **nie ma jeszcze twardego dowodu**, że:

* każdy moduł w łańcuchu można wygodnie adresować osobno z własnego kodu,
* istnieje kompletna, elegancka dokumentacja API do takiego sterowania.

Na razie są mocne przesłanki, ale nie pełne potwierdzenie.

## Dlaczego inne opcje wypadły słabiej

### Govee Mini Panels

Plusy:

* dobrze wyglądają
* mają LAN API

Minusy:

* minimum 10 paneli w zestawie
* brak pewności, czy każdy panel można traktować jako niezależny kanał logiczny
* duże ryzyko, że sterowanie działa głównie na poziomie całego urządzenia

### WiZ Light Bars

Plusy:

* lokalne sterowanie wygląda sensowniej
* gotowy produkt

Minusy:

* drogo przy 5–6 niezależnych punktach
* dużo światła i spore gabaryty
* mniej przypomina kompaktowy „sygnalizator procesu”, bardziej zwykłą lampę

### DIY na gołych diodach

Plusy:

* duża elastyczność
* technicznie da się zrobić bardzo granularne sterowanie

Minusy:

* estetyka zależy od obudowy
* bez gotowej ładnej obudowy to mnie średnio interesuje
* nie chcę iść w rozwiązanie, które wygląda jak prototyp z luźnych komponentów

## Aktualna decyzja

Na teraz najlepszy trop do dalszego sprawdzania to:

**Yeelight Cube / Matrix jako 5–6 modułów, gdzie każdy moduł reprezentuje osobny proces.**

To jest obecnie najbardziej rokujące połączenie:

* estetyki
* modularności
* potencjalnego sterowania przez API
* sensownego rozmiaru na biurko

## Co warto dalej sprawdzić

1. czy ktoś już sterował Yeelight Cube z własnego kodu
2. czy da się sterować osobno modułami w zestawie
3. czy da się ustawiać kolor / wzór per moduł
4. czy istnieją przykłady użycia z Home Assistant, GitHub albo forów
5. czy lokalne sterowanie wymaga chmury, czy działa całkowicie w LAN

## Robocza koncepcja użycia w StandPulse

* moduł 1: proces A
* moduł 2: proces B
* moduł 3: proces C
* moduł 4: proces D
* moduł 5: proces E
* moduł 6: proces F albo stan globalny

Przykładowe kolory stanów:

* zielony – OK
* żółty – w toku
* niebieski – czeka
* czerwony – błąd / alarm
* biały – neutralny
* miganie – stan wymagający uwagi

Na to może się nakładać alarm zasiedzenia – wtedy wszystkie moduły zaczynają migać jednocześnie.
