# Codex hooks — notatki z PoC

## 1. Cel testu

Sprawdzamy, czy lokalne hooki Codexa mogą być użyte jako bardziej niezawodny sygnał stanu agenta niż ręczne wywoływanie komend typu `branch step-begin` i `branch step-end`.

Interesuje nas przede wszystkim prosty model:

1. agent dostał zadanie i zaczyna turę,
2. agent zakończył turę,
3. opcjonalnie w przyszłości: agent czeka na reakcję użytkownika.

Na tym etapie nie chcemy śledzić każdego użycia narzędzia przez agenta.

## 2. Aktualna konfiguracja lokalna

Hooki są włączone lokalnie dla repozytorium przez:

```text
.codex/config.toml
```

Aktualnie aktywne są tylko:

```text
SessionStart
UserPromptSubmit
Stop
```

Wyłączone zostały:

```text
PreToolUse
PostToolUse
PermissionRequest
```

Powód: `PreToolUse` i `PostToolUse` są bardzo głośne, bo odpalają się przy każdym narzędziu użytym przez agenta. Dla monitora procesu agenta to zbyt niski poziom szczegółowości.

## 3. Pliki PoC

Konfiguracja:

```text
.codex/config.toml
```

Skrypt hooka:

```text
hooks/codex-hook-log.ps1
```

Opis loggera:

```text
hooks/README.md
```

Logi robocze:

```text
hooks/logs/
```

Katalog `hooks/logs/` jest ignorowany przez Git.

## 4. Jak działa logger

Codex uruchamia skonfigurowany skrypt PowerShell i przekazuje payload hooka jako JSON na stdin.

Skrypt:

1. czyta JSON ze stdin,
2. parsuje payload,
3. zapisuje wpis do `hooks/logs/YYYY-MM-DD-all.jsonl`,
4. zapisuje wpis do pliku per event, np. `YYYY-MM-DD-Stop.jsonl`,
5. zapisuje ostatni payload jako `last.json`,
6. zapisuje ostatni payload danego typu jako `last-Stop.json`, `last-UserPromptSubmit.json` itd.

Logger celowo nic nie wypisuje na stdout. Codex traktuje stdout hooka jako odpowiedź sterującą, więc zwykłe logowanie powinno iść tylko do plików.

Skrypt ustawia UTF-8 dla wejścia/wyjścia i ma retry przy zapisie plików. Retry było potrzebne, bo gdy aktywne były hooki narzędziowe, kilka hooków mogło zapisywać logi równolegle.

## 5. Znaczenie aktualnych hooków

### `SessionStart`

Odpala się przy starcie albo wznowieniu sesji Codexa.

W testach payload zawierał m.in.:

```text
session_id
transcript_path
cwd
model
permission_mode
source
```

Przykładowe `source`:

```text
resume
```

Ten hook jest pomocniczy. Nie oznacza początku każdej tury pracy agenta.

### `UserPromptSubmit`

Odpala się po wysłaniu wiadomości przez użytkownika.

To jest najlepszy aktualny sygnał:

```text
agent dostał zadanie / zaczyna się tura
```

Payload zawiera m.in.:

```text
session_id
turn_id
transcript_path
cwd
model
permission_mode
prompt
```

Dla monitora procesu ten event może ustawiać stan:

```text
Working
```

### `Stop`

Odpala się, gdy agent kończy turę.

Payload zawiera m.in.:

```text
session_id
turn_id
transcript_path
cwd
model
permission_mode
last_assistant_message
stop_hook_active
```

Dla monitora procesu ten event może ustawiać stan:

```text
Idle / Done
```

`last_assistant_message` jest ważne, bo daje dostęp do końcowej odpowiedzi agenta. W przyszłości można z niego wyciągać opis wykonanych zmian.

## 6. Obserwacje z testów

1. Hooki działają lokalnie w repozytorium po restarcie Codexa.
2. Codex pokazuje hooki w UI jako osobne zdarzenia.
3. Zmiana `.codex/config.toml` nie przeładowuje aktywnej konfiguracji w już działającej sesji. Po zmianie listy hooków trzeba zrestartować Codexa/plugin.
4. `UserPromptSubmit` zapisuje się w logach, ale UI może eksponować go mniej widocznie niż `Stop`.
5. Stare wpisy `PreToolUse` i `PostToolUse` mogą zostać w historii rozmowy i logach, ale po restarcie z aktualną konfiguracją nie powinny już dopisywać się nowe.
6. Gdy `PreToolUse` był aktywny, narzędzie shellowe było raportowane jako `tool_name: "Bash"`, mimo że faktyczne komendy były PowerShellowe. Komenda PowerShell była widoczna w `tool_input.command`.

## 7. Planowany model stanu monitora

Minimalny model dla StandPulse / monitora agentów:

```text
SessionStart      -> sesja Codexa uruchomiona albo wznowiona
UserPromptSubmit  -> agent zaczyna turę, stan Working
Stop              -> agent kończy turę, stan Done / Idle
```

W przyszłości można dodać:

```text
PermissionRequest -> agent czeka na zgodę użytkownika
```

Na razie zostaje wyłączone, żeby nie komplikować PoC.

## 8. Pomysł: blokowanie `Stop`, gdy brakuje danych

W Codex hooks `Stop` może nie tylko logować zdarzenie, ale też zablokować zakończenie tury.

To nie jest cofnięcie zmian w plikach.
To jest raczej zawrócenie agenta zanim tura zostanie uznana za zakończoną.

Mechanizm:

1. Agent kończy odpowiedź.
2. Odpala się hook `Stop`.
3. Skrypt analizuje payload, np. `last_assistant_message`.
4. Jeśli brakuje wymaganych danych, hook zwraca blokadę.
5. Codex dostaje powód blokady jako prompt kontynuacyjny.
6. Agent musi uzupełnić odpowiedź.
7. `Stop` odpala się ponownie.

Przykładowe zastosowanie dla workflow brancha:

```text
Wymagaj, żeby końcowa odpowiedź agenta zawierała krótki opis zmian.
```

Można ustalić format, np.:

```text
BRANCH_STEP_SUMMARY: Krótki opis wykonanych zmian
```

Jeśli `Stop` nie znajdzie takiej linii w `last_assistant_message`, może zablokować zakończenie tury i zwrócić agentowi komunikat:

```text
Dodaj linię BRANCH_STEP_SUMMARY z krótkim opisem wykonanych zmian.
```

Technicznie `Stop` może zablokować turę na dwa sposoby.

Wariant JSON na stdout:

```json
{
  "decision": "block",
  "reason": "Dodaj linię BRANCH_STEP_SUMMARY z krótkim opisem wykonanych zmian."
}
```

Wariant przez kod wyjścia:

```text
exit 2
```

i komunikat na stderr:

```text
Dodaj linię BRANCH_STEP_SUMMARY z krótkim opisem wykonanych zmian.
```

Ważna uwaga: zwykły logger nie powinien nic pisać na stdout, bo stdout jest kanałem sterującym. Dopiero specjalny hook walidujący `Stop` powinien zwracać JSON sterujący albo kod wyjścia `2`.

## 9. Możliwe użycie z `branch`

Docelowo hooki mogą zastąpić część ręcznych wywołań w workflow `branch`.

Możliwy kierunek:

```text
SessionStart      -> rezerwacja / powiązanie sesji agenta
UserPromptSubmit  -> oznaczenie agenta jako Working
Stop              -> pobranie opisu z odpowiedzi i zamknięcie kroku agenta
```

Na tym etapie nie jest jeszcze przesądzone, czy `branch step-begin` powinien odpalać się na `UserPromptSubmit`.
To zależy od tego, czy każda tura użytkownika oznacza realne zmiany w plikach.

Bezpieczniejszy wariant przyszłościowy:

1. `UserPromptSubmit` ustawia stan agenta na `Working` w monitorze.
2. Dopiero `Stop` sprawdza, czy były zmiany w repozytorium.
3. Jeśli były zmiany, `Stop` wymaga `BRANCH_STEP_SUMMARY`.
4. Po poprawnym opisie uruchamiane jest `branch step-end` albo analogiczny mechanizm zamknięcia pracy.

To wymaga osobnego projektu integracji, ale PoC pokazuje, że dane potrzebne do takiego mechanizmu są dostępne.

## 10. Identyfikacja agentów

Hooki Codexa dają stabilny techniczny identyfikator sesji:

```text
session_id
```

Dodatkowo payload zawiera:

```text
turn_id
cwd
transcript_path
```

Proponowany model:

```text
agent instance = session_id
tura pracy = turn_id
repozytorium = cwd
```

Imię agenta powinno być przyjazną etykietą dla człowieka, a nie jedynym źródłem identyfikacji.
W praktyce system może utrzymywać mapowanie:

```text
session_id -> agentName
```

albo pełniej:

```json
{
  "sessionId": "019dbebe-49f6-70c2-82fd-279d22b57325",
  "agentName": "Jurek",
  "repoPath": "p:\\myRules",
  "transcriptPath": "C:\\Users\\Krzysztof\\.codex\\sessions\\...",
  "state": "working",
  "lastTurnId": "019dc006-31cd-7cf0-8241-254465003396"
}
```

Przy pierwszym `SessionStart` dla nowego `session_id` system powinien sprawdzić, czy sesja ma już przypisane imię.
Jeżeli nie, może:

1. automatycznie przydzielić imię według reguł repozytorium,
2. zapisać przypisanie `session_id -> agentName`,
3. przypominać agentowi albo użytkownikowi, pod jakim imieniem działa dana sesja.

Przypominanie imienia jest ważne ergonomicznie.
Jeśli agent w odpowiedzi albo monitorze pojawia się jako `Jurek`, użytkownik szybciej kojarzy, która sesja pracuje i w jakim kontekście.

## 11. Stałe imiona per repozytorium

Losowe przydzielanie imion globalnie może wprowadzić chaos.
Jeżeli raz `Jurek` pracuje w `myRules`, a innym razem w `CRMWeb`, to komunikat:

```text
Jurek skończył pracę
```

przestaje być natychmiast czytelny.

Lepszy model to zdefiniowanie obsady agentów per repozytorium.
Przykład:

```json
{
  "repos": [
    {
      "repoPath": "p:\\myRules",
      "repoName": "myRules",
      "color": "#F97316",
      "agentSlots": [
        { "slot": 1, "name": "Jurek" },
        { "slot": 2, "name": "Mila" },
        { "slot": 3, "name": "Leon" }
      ]
    },
    {
      "repoPath": "p:\\StandPulse",
      "repoName": "StandPulse",
      "color": "#22C55E",
      "agentSlots": [
        { "slot": 1, "name": "Nina" },
        { "slot": 2, "name": "Tomek" }
      ]
    }
  ]
}
```

Wtedy reguła może być prosta:

```text
pierwsza aktywna sesja Codexa dla danego repo -> slot 1 -> Jurek
druga aktywna sesja Codexa dla danego repo -> slot 2 -> Mila
trzecia aktywna sesja Codexa dla danego repo -> slot 3 -> Leon
```

Dzięki temu komunikaty stają się stabilne poznawczo:

```text
Jurek pracuje
Jurek skończył
Jurek czeka na użytkownika
```

Jeżeli `Jurek` zawsze oznacza pierwszego agenta w `myRules`, użytkownik nie musi za każdym razem czytać pełnego kontekstu.

## 12. `sessions.json` jako źródło przypisań

Plik `sessions.json` może przechowywać zarówno aktywne sesje, jak i przypisania imion do repozytoriów.

Roboczy podział:

```text
repo configuration -> jakie sloty i kolory ma repo
active sessions    -> które session_id zajmuje który slot
```

Przykładowa struktura:

```json
{
  "repoProfiles": [
    {
      "repoPath": "p:\\myRules",
      "repoName": "myRules",
      "color": "#F97316",
      "agentSlots": [
        { "slot": 1, "name": "Jurek" },
        { "slot": 2, "name": "Mila" }
      ]
    }
  ],
  "activeSessions": [
    {
      "sessionId": "019dbebe-49f6-70c2-82fd-279d22b57325",
      "repoPath": "p:\\myRules",
      "repoName": "myRules",
      "slot": 1,
      "agentName": "Jurek",
      "state": "working",
      "startedAt": "2026-04-24T17:01:06+02:00",
      "stateChangedAt": "2026-04-24T17:05:25+02:00",
      "lastTurnId": "019dc006-31cd-7cf0-8241-254465003396",
      "transcriptPath": "C:\\Users\\Krzysztof\\.codex\\sessions\\..."
    }
  ]
}
```

To daje dwa ważne efekty:

1. stabilne imiona agentów w obrębie repo,
2. możliwość stabilnego kolorowania komunikatów per repo.

## 13. Automatyczne tworzenie profilu repo

Nie każde repozytorium musi być konfigurowane ręcznie z góry.
Program może tworzyć profil repo automatycznie przy pierwszym `SessionStart` z nieznanego `cwd`.

Proponowana reguła:

```text
jeśli cwd nie ma jeszcze profilu repo:
  utwórz repoProfile
  wybierz pierwszy wolny kolor z puli kolorów
  wybierz pierwsze wolne imiona z globalnej puli imion
  przypisz pierwszego agenta do slotu 1
```

Przykład:

```json
{
  "availableNames": ["Jurek", "Mila", "Leon", "Nina", "Tomek"],
  "availableColors": ["#F97316", "#22C55E", "#3B82F6", "#EAB308"]
}
```

Gdy zgłasza się pierwsza sesja z nowego repo, system może automatycznie dopisać:

```json
{
  "repoPath": "p:\\NewProject",
  "repoName": "NewProject",
  "color": "#3B82F6",
  "agentSlots": [
    { "slot": 1, "name": "Leon" },
    { "slot": 2, "name": "Nina" },
    { "slot": 3, "name": "Tomek" }
  ]
}
```

Dzięki temu nie trzeba konfigurować każdego nowego repo ręcznie, a jednocześnie po pierwszym przydziale repo dostaje stabilne imiona i kolor.

Ważna zasada:

```text
losowość może wystąpić tylko przy pierwszym utworzeniu profilu repo
po utworzeniu profilu przypisania powinny być stabilne
```

Jeszcze lepszy wariant to brak losowości: program bierze kolejne wolne imiona i kolory z uporządkowanej listy.
Wtedy zachowanie jest przewidywalne i łatwe do odtworzenia.

Jeżeli później użytkownik uzna, że dane repo powinno mieć inne imiona albo kolor, może ręcznie poprawić profil repo w `sessions.json` lub w osobnym pliku konfiguracyjnym.

## 14. Kolory repozytoriów

Kolor repozytorium powinien być stałym sygnałem wizualnym.

Przykład:

```text
pomarańczowy -> myRules
zielony      -> StandPulse
niebieski    -> CRMWeb
```

Dzięki temu monitor może komunikować stan bez konieczności czytania całego tekstu.
Jeśli pomarańczowy ekran albo pasek miga, użytkownik od razu wie, że chodzi o `myRules`.

To jest szczególnie przydatne, gdy równolegle działa kilku agentów w kilku repozytoriach.
Kolor repo i imię agenta wzmacniają się nawzajem:

```text
pomarańczowy + Jurek = pierwszy agent w myRules
pomarańczowy + Mila  = drugi agent w myRules
zielony + Nina       = pierwszy agent w StandPulse
```

Ten model powinien być znacznie czytelniejszy niż globalna pula losowo przydzielanych imion.
