# Branch Notifier

Używaj tego pliku jako krótkiej dokumentacji pomocniczej dla skilla `branch` w zakresie konfiguracji powiadomień workflow.

## Rola tego pliku

Program `branch` może wysyłać powiadomienia po `branch step-end`. Robi to przez zewnętrzny skrypt `work-notify.ps1`, jeśli jest dostępny w `PATH`.

Plik `.workai\notification-config.json` jest tworzony automatycznie przy pierwszym `branch step-end`, jeśli taki plik jeszcze nie istnieje.

Powiadomienia są best-effort: jeśli `work-notify.ps1` nie jest dostępny albo samo powiadomienie się nie uda, `step-end` nie powinien przerwać pracy.

Użytkownik może później chcieć zmodyfikować te ustawienia. Może Cię poprosić o pomoc w zmianie domyślnych ustawień i dobrze by było wiedzieć, jak to zrobić. Dlatego poniżej znajdziesz dokumentację parametrów notyfikacji.

Komendy `branch start`, `branch step-begin`, `branch flush` i `branch finish` nie wysyłają zwykłych powiadomień.

## Domyślna struktura pliku

```json
{
  "notifications": {
    "minimumStepSecondsForPopup": 15,
    "minimumStepSecondsForPhone": 120,
    "sendToPhone": true,
    "sound": "done",
    "silent": false,
    "duration": "short",
    "ignoreWindowTitlePatterns": [
      "Visual Studio Code",
      "Codex",
      "Claude Code"
    ]
  }
}
```

## Znaczenie pól

### `notifications.minimumStepSecondsForPopup`

Minimalny czas kroku liczony jako `branch step-end - branch step-begin`, od którego workflow może pokazać lokalny popup.

Przykład:

- `15` oznacza, że bardzo krótkie kroki poniżej 15 sekund nie muszą pokazywać lokalnego popupu.

### `notifications.minimumStepSecondsForPhone`

Minimalny czas kroku liczony jako `branch step-end - branch step-begin`, od którego workflow może dodatkowo wysłać powiadomienie na telefon.

Przykład:

- `120` oznacza, że wysyłka na telefon zaczyna mieć sens dopiero od kroków trwających co najmniej 2 minuty.

Ważna reguła:

- ta wartość nie może być mniejsza niż `minimumStepSecondsForPopup`.

### `notifications.sendToPhone`

Flaga włączająca albo wyłączająca wysyłkę na telefon przez `ntfy`.

Przykład:

- `true` - workflow może wysyłać powiadomienia na telefon,
- `false` - workflow ogranicza się do lokalnych powiadomień.

### `notifications.sound`

Nazwa dźwięku przekazywana do `work-notify.ps1`.

Najczęściej podaje się nazwę pliku WAV bez rozszerzenia, na przykład:

- `done`
- `alert`

Jeśli użytkownik chce zmienić dźwięk, źródłem prawdy dla dostępnych nazw są pliki `.wav` w katalogu `tools\EndWorkNotifier\sounds`.

### `notifications.silent`

Jeśli `true`, lokalny popup ma być pokazany bez dźwięku.

Ważna reguła:

- `silent = true` nie może być użyte jednocześnie z ustawionym `sound`.

### `notifications.duration`

Czas widoczności lokalnego toastu.

Dozwolone wartości:

- `short`
- `long`

### `notifications.ignoreWindowTitlePatterns`

Lista regexów dla tytułu aktywnego okna.

Jeśli aktywne okno pasuje do któregoś wzorca, workflow może pominąć zwykły popup.

To służy do ograniczenia zbędnych powiadomień wtedy, gdy użytkownik i tak patrzy już na właściwe narzędzie albo okno pracy.

## Jak rozumieć tę konfigurację

- `branch step-end` tworzy sensowne wartości domyślne przy pierwszym użyciu powiadomień; użytkownik nie musi ich zmieniać od razu.
- Czas kroku liczony jest jako różnica między `branch step-begin` i `branch step-end` dla danego kroku agenta.
- Agent ma umieć wyjaśnić działanie tych pól i pomóc je poprawić, ale nie powinien zgadywać nieistniejących opcji poza tym formatem.
