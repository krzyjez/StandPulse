---
name: init
description: Inicjalizuje pracę w repozytorium StandPulse na początku sesji albo po resecie kontekstu. Używaj, gdy użytkownik prosi o init, zapoznanie się z projektem, zbudowanie krótkiego WORKING MEMORY albo szybkie odświeżenie źródeł prawdy bez zapisywania czegokolwiek.
version: 3
modified-date: 2026-05-05
---

# Init

Zbuduj krótki i użyteczny kontekst roboczy dla bieżącej sesji w repozytorium StandPulse.

Nie streszczaj całego projektu. Zrozum tylko tyle, ile potrzeba, żeby bezpiecznie przejść do aktualnego zadania.

## Workflow

1. Na początku wykonaj `agent-signal get-name` i zapamiętaj zwrócone imię jako identyfikator agenta dla tej sesji.
2. Jeśli istnieje `project-memory.md`, przeczytaj go jako pierwsze źródło trwałego kontekstu.
3. Jeśli istnieje `todo.md`, przeczytaj go w zakresie otwartych tematów roboczych.
4. Czytaj `version.md` tylko wtedy, gdy użytkownik pyta o wersję, ostatnie zmiany albo release notes.
5. Zmapuj zadanie użytkownika do właściwego obszaru:
   - domena i założenia MVP: `docs/StandPulse-notatki.md`,
   - komunikacja lokalna sensora: `docs/StandPulse-komunikacja-sensora.md`,
   - firmware i aktualne pliki urządzenia: `docs/StandPulse-firmware-sensor.md`,
   - aplikacja podglądu sensora: `src/StandPulse.ScannerViewer`,
   - firmware urządzenia: `firmware/standpulse_sensor`.
6. Przy zadaniach C# najpierw uwzględnij reguły z `src/StandPulse.ScannerViewer/AGENTS.md`.
7. Przy zadaniach firmware czytaj tylko właściwy szkic `.ino` i dokument firmware, chyba że użytkownik poprosi o szerszą analizę.
8. Jeśli repo korzysta z workflow brancha, używaj tego samego imienia agenta w `branch step-begin <name>` i `branch step-end <name> "<description>"`.

## Output Contract

W odpowiedzi wypisz sekcję `WORKING MEMORY` w maksymalnie 10-15 krótkich liniach.

Uwzględnij tylko:

- `Agent` - imię agenta z `agent-signal get-name`, jeśli komenda działa.
- `Sources Of Truth` - najważniejsze dokumenty lub katalogi dla bieżącego zadania.
- `Invariants` - tylko trwałe reguły domenowe albo techniczne istotne dla zadania.
- `Scope` - jedna linia z zakresem sesji.
- `Watchouts` - najwyżej 1-3 ryzyka lub pułapki, jeśli wynikają z lokalnej pamięci albo dokumentacji.

Jeśli użytkownik nie podał zadania, wpisz dosłownie:

`Scope: (nie podano zadania - init only)`

## Reading Rules

- Nie czytaj całego repo podczas initu.
- Nie czytaj katalogów `dist`, `bin`, `obj` ani artefaktów builda.
- Nie czytaj całego katalogu `docs`; wybierz minimalny zestaw dokumentów pasujący do zadania.
- Nie dubluj całych sekcji dokumentacji; wyciągaj tylko robocze wnioski.
- Przy konflikcie między notatkami roboczymi a aktualnymi dokumentami w `docs`, traktuj `docs` jako silniejsze źródło prawdy, chyba że `project-memory.md` świadomie opisuje wyjątek.

## Guardrails

- Nie twórz żadnych plików podczas samego `init`.
- Nie zapisuj nic do `project-memory.md`, `todo.md` ani `version.md`.
- Nie opisuj użytkownikowi całego projektu, jeśli prosi tylko o wejście w kontekst.
- Nie zakładaj, że pliki w `dist` są źródłem prawdy; to artefakty wynikowe.
- Po `WORKING MEMORY` przejdź od razu do realizacji zadania, jeśli użytkownik o nie poprosił.
