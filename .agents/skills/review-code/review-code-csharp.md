# Review Code - C#/.NET

Używaj tego pliku jako dodatkowej perspektywy do `review-code`, gdy wykryty stack obejmuje C# albo .NET.

Nie próbuj robić pełnego audytu całego ekosystemu .NET. Skup się na tych punktach, które realnie zwiększają ryzyko błędów, regresji albo kosztu utrzymania.

## Priorytet 1: kontrakty, nullability i intencja API

Sprawdź:

- czy kontrakty publicznych metod, klas i DTO są jednoznaczne,
- czy adnotacje nullability pasują do rzeczywistego zachowania kodu,
- czy kod nie nadużywa operatora `!` do uciszania ostrzeżeń zamiast usunięcia problemu,
- czy publiczne wejścia wykonują sensowne sprawdzenia w runtime, zamiast polegać wyłącznie na ostrzeżeniach kompilatora,
- czy inicjalizacja wymaganych danych jest wymuszona w czytelny sposób.

Szczególnie zwracaj uwagę na miejsca, gdzie typ wygląda na nie-nullowalny, ale kod nadal może dopuścić `null` przez deserializację, mapowanie albo dane z zewnątrz.

## Priorytet 2: dopasowanie do wersji języka i nowoczesności kodu

Sprawdź:

- jaką wersję C# i jakie możliwości języka realnie dopuszcza projekt,
- czy kod wygląda jak pisany dla tej wersji języka, a nie dla dużo starszego C#,
- czy implementacja nie wraca bez potrzeby do prehistorycznych wzorców, które dziś są mniej czytelne albo mniej bezpieczne,
- czy dostępne w projekcie mechanizmy, takie jak nullability, LINQ albo nowsze konstrukcje języka, nie są ignorowane bez wyraźnego powodu,
- czy użyty styl nie powoduje realnego kosztu przez rozwlekłość, gorszy kontrakt, słabszą czytelność albo pominięcie ważnych mechanizmów języka dostępnych w projekcie.

Nie chodzi o wymuszanie nowinek dla samych nowinek ani o zgłaszanie uwag tylko dlatego, że istnieje nowsza składnia. Chodzi o wychwycenie kodu, który wygląda jak napisany kilka wersji języka temu i przez to jest bardziej rozwlekły, mniej czytelny, ma słabszy kontrakt albo gorzej wykorzystuje gwarancje dostępne w projekcie.

Jeśli projekt świadomie używa starszego stylu albo ogranicza się do starszej wersji C#, respektuj to. Problem zgłaszaj wtedy, gdy kod jest starszy niż standard projektu albo ignoruje dostępne mechanizmy języka w sposób, który realnie pogarsza wynik.

## Priorytet 3: async, await i anulowanie

Sprawdź:

- czy operacje I/O są naprawdę asynchroniczne, zamiast blokować wątek przez `.Result`, `.Wait()` albo podobne obejścia,
- czy `async void` nie jest użyte poza prawdziwymi handlerami zdarzeń,
- czy długie albo zewnętrzne operacje przyjmują i propagują `CancellationToken`,
- czy anulowanie dotyczy operacji, a nie jest tylko dekoracją w sygnaturze,
- czy `CancellationTokenSource` jest poprawnie zwalniany i nie jest używany ponownie po anulowaniu,
- czy kod nie uruchamia ognia i zapomnienia bez kontroli błędów i cyklu życia.

W review traktuj sync-over-async na ścieżce requestu, pracy w tle albo integracji z zewnętrznym systemem jako mocny sygnał ostrzegawczy.

## Priorytet 4: obsługa błędów i API HTTP

Sprawdź:

- czy kod łapie tylko wyjątki, które naprawdę potrafi sensownie obsłużyć,
- czy wyjątki nie są połykane, spłaszczane albo zamieniane na puste wartości bez śladu diagnostycznego,
- czy odpowiedzi błędów HTTP są spójne i nadają się do diagnozowania,
- czy kod API nie ujawnia szczegółów wyjątków w środowisku produkcyjnym,
- czy przy API HTTP zachowany jest sensowny kontrakt błędów, na przykład `ProblemDetails`, gdy pasuje do projektu,
- czy logowanie zachowuje kontekst błędu bez wycieku sekretów, tokenów i danych wrażliwych.

Jeśli widzisz szczegółowe dane wyjątków wystawiane publicznie poza środowiskiem developerskim, traktuj to jako problem wysokiej wagi.

## Priorytet 5: bezpieczeństwo i dane

Sprawdź:

- czy w kodzie nie ma zahardkodowanych sekretów, kluczy, tokenów albo parametrów kryptograficznych,
- czy kod nie wymusza przestarzałych ustawień bezpieczeństwa albo nie omija bezpiecznych domyślnych zachowań platformy,
- czy operacje na plikach, archiwach i ścieżkach nie otwierają drogi do path traversal,
- czy zapytania, filtry i komendy do zewnętrznych systemów nie są budowane przez niebezpieczną konkatenację,
- czy logika autoryzacji jest wykonywana na granicy wejścia, a nie tylko głęboko w implementacji,
- czy zmiany w modelach i migracjach nie grożą cichą utratą albo zniekształceniem danych.

Analityczne reguły bezpieczeństwa z analyzerów traktuj jako mocny sygnał. Jeśli kod je wycisza, sprawdź czy istnieje rzeczywiste i dobrze opisane uzasadnienie.

## Priorytet 6: architektura i lifetimes

Sprawdź:

- czy logika biznesowa nie jest upychana w kontrolerach, endpointach, handlerach HTTP, mapperach albo encjach ORM,
- czy zależności są wstrzykiwane zgodnie z ich rzeczywistym cyklem życia,
- czy singleton nie trzyma stanu albo zależności o węższym lifetime,
- czy warstwy są czytelnie rozdzielone: transport, orkiestracja, domena, infrastruktura,
- czy abstrakcje rozwiązują realny problem, a nie tylko poprawiają wygląd kodu,
- czy zmiana nie rozsiewa wiedzy o jednym szczególe po wielu modułach.

Jeśli projekt ma lokalne reguły C#, respektuj je przed ogólnymi preferencjami stylu.

## Priorytet 7: czytelność, idiomy języka i komentarze

Sprawdź:

- czy kod jest prosty do przeczytania bez śledzenia wielu pośrednich warstw,
- czy nazwy oddają odpowiedzialność i zakres skutków ubocznych,
- czy użyte nowoczesne cechy C# poprawiają czytelność zamiast ją pogarszać,
- czy wyjątki mają sensowne, konkretne typy,
- czy kolekcje, LINQ i projekcje nie ukrywają kosztownej albo nieoczywistej logiki,
- czy przypadkowa złożoność nie została zamaskowana wzorcami "enterprise looking code",
- czy klasy mają krótki komentarz wyjaśniający czym są i po co istnieją,
- czy publiczne metody mają komentarz XML opisujący kontrakt,
- czy prywatne metody mają co najmniej krótki komentarz zdaniowy wtedy, gdy ich intencja nie jest oczywista z samej nazwy i treści.

Nie rób uwag tylko dlatego, że kod nie wygląda jak Twój ulubiony styl. Szukaj momentów, gdzie styl utrudnia zrozumienie lub maskuje ryzyko.

Brak komentarzy traktuj jako realny problem utrzymywalności wtedy, gdy kod traci przez to czytelność, kontrakt publiczny staje się mniej jasny albo klasa nie wyjaśnia swojej roli. Dla klas i metod publicznych oczekuj tego bardziej konsekwentnie niż dla prostych metod prywatnych.

## Priorytet 8: testy

Sprawdź:

- czy testy weryfikują kontrakt i zachowanie, a nie prywatne szczegóły implementacyjne,
- czy obejmują scenariusze błędów, `null`, puste dane, anulowanie, timeouty i przypadki graniczne,
- czy testy asynchroniczne naprawdę czekają na zakończenie pracy,
- czy mocki nie fałszują rzeczywistego kontraktu z infrastrukturą,
- czy przy zmianach w API, serializacji albo danych istnieją testy chroniące kontrakt.

Brak testów dla ryzykownych zmian w mapowaniu, walidacji, autoryzacji, współbieżności albo obsłudze błędów zwykle traktuj co najmniej jako problem ważny.

## Sygnały wysokiego ryzyka

Szczególnie wypatruj:

- sync-over-async na ścieżce requestu,
- kodu wyglądającego jak napisany dla dużo starszej wersji C#, mimo że projekt pozwala na nowocześniejsze i czytelniejsze rozwiązanie,
- tłumienia ostrzeżeń nullability bez naprawy przyczyny,
- łapania `Exception` bez wyraźnej potrzeby,
- publicznego wypuszczania szczegółów wyjątków,
- ręcznego obchodzenia bezpieczeństwa platformy,
- zależności o niepoprawnym lifetime,
- logiki biznesowej rozlanej po warstwach transportowych,
- klas i metod publicznych bez sensownej dokumentacji,
- testów, które przechodzą tylko dlatego, że omijają realne warunki błędu.

## Jak raportować findings dla C#/.NET

Jeśli dany problem wynika z reguły specyficznej dla .NET, nazwij to wprost w treści uwagi, na przykład:

- kontrakt nullability nie zgadza się z zachowaniem runtime,
- kod ignoruje możliwości wersji C# używanej w projekcie i wraca do starszych, bardziej rozwlekłych wzorców,
- operacja I/O nie propaguje `CancellationToken`,
- API ujawnia szczegóły wyjątków zamiast zwracać spójny kontrakt błędu,
- publiczny kontrakt klasy albo metody nie ma wymaganej dokumentacji,
- lifetime zależności grozi błędem współbieżności albo wyciekiem stanu.
