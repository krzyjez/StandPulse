# Review w aplikacji Codex

## Recenzent i kontekst

Główny agent odpowiada za przygotowanie zakresu przez `branch review` i zapis wyniku przez `--complete`. Ocenę wykonuje osobny subagent aplikacji, uruchomiony ze świeżym kontekstem, bez historii implementacji. Jeśli narzędzie delegowania udostępnia wybór dziedziczenia rozmowy, wybierz brak dziedziczenia (np. `fork_turns: "none"`). Nie wybieraj innego modelu bez ustaleń użytkownika.

Przekaż recenzentowi:

1. Bezwzględną ścieżkę repozytorium, cel zadania, wymagania użytkownika i kryteria akceptacji, także te niezapisane w dokumentacji.
2. `HeadCommit`, `BaseCommit`, `MergeBaseCommit` zwrócone przez CLI. Zakres to cały `MergeBaseCommit..HeadCommit`, z oceną integracji z `BaseCommit`; nie tylko ostatni commit ani niezacommitowane zmiany.
3. Polecenie przeczytania obowiązujących `AGENTS.md` oraz lokalnego skilla `init-session`, jeśli istnieje, i wykonania jego części zbierającej kontekst bez zapisów. To ma być skill projektu docelowego, nie `init-session` z `P:\ai`. Bez niego czytaj README, pamięć projektu i dokumentację właściwą dla zmienianego obszaru.
4. Ścieżkę dostępnego lokalnie `review-code`, jeśli jest używany, oraz istotne instrukcje technologiczne. Gdy brak tego skilla, stosuj kryteria z głównej instrukcji `branch review`. Subagent nie musi mieć skilla na liście narzędzi: może odczytać jego plik.
5. Zakaz dalszego delegowania, poprawiania kodu, zmieniania stanu Git, publikowania komentarzy i zapisywania raportu. Sprawdzenia nie mogą modyfikować repozytorium; jeśli wymagają zapisów, wskaż ograniczenie albo użyj uzgodnionego środowiska izolowanego.
6. Kontrakt odpowiedzi: zastosuj sekcję „Zwięzły raport review” z `branch-techdocs.md`; przekaż ją recenzentowi. Każda uwaga ma wagę i rodzaj (`strategiczne` albo `techniczne`) - od rodzaju zależy, czy rozstrzyga ją użytkownik, czy agent. W razie braku dostępu do kodu lub istotnej części zakresu zgłoś nieukończoną ocenę zamiast wyniku bez uwag.

Główny agent czeka na zakończenie recenzenta i sprawdza kompletność raportu. Zapisuje jego rzeczywisty identyfikator w `--reviewer`; CLI umieszcza go w metadanych YAML. Nie zastępuje niezależnej oceny własną deklaracją. Nieukończony lub przerwany przegląd nie może być zarejestrowany jako `passed`; pozostaw go nieukończonym. Po zmianie zakresu uruchom nową ocenę. Główny agent sprawdza także HEAD i stan roboczy przed oraz po review; przy nieoczekiwanych zmianach zatrzymuje rejestrację bez automatycznego cofania. Zakaz zapisu w promptcie nie jest gwarancją sandboxa. Brak opcjonalnego sprawdzenia opisz jako ograniczenie; brak oceny istotnej części zakresu oznacza `incomplete`.

## Wbudowane review OpenAI

Subagent aplikacji z instrukcją projektu nie jest tym samym mechanizmem co wbudowane `/review`. Nie oznaczaj go jako natywnego review OpenAI.

OpenAI dokumentuje `/review` w aplikacji, ale polecenie użytkownika nie musi być dostępne jako narzędzie agenta. Preferuj wbudowany mechanizm, jeśli środowisko rzeczywiście umożliwia jego wywołanie i odebranie kompletnego raportu dla ustalonego zakresu. Samo napisanie `/review` w promptcie subagenta nie dowodzi jego uruchomienia.

`codex exec review` jest osobnym procesem CLI. Nie uruchamiaj go automatycznie zamiast subagenta aplikacji. Jeśli użytkownik wyraźnie wymaga wbudowanego review, a dostępny jest tylko zwykły subagent, przedstaw różnicę i uzyskaj decyzję o tej alternatywie. Błąd narzędzi lub blokada uprawnień recenzenta nie oznaczają poprawnego kodu; nie wyłączaj zabezpieczeń w celu dokończenia oceny.

Dokumentacja producenta: [review](https://learn.chatgpt.com/docs/code-review?surface=app), [subagenci](https://learn.chatgpt.com/docs/agent-configuration/subagents).
