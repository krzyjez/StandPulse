# agent-behavior v.8

## Zmiany w plikach

1. Polecenia analityczne nie oznaczają zgody na zapis. Polecenia wykonawcze oznaczają zgodę tylko na zmiany małe i oczywiste.

2. Mała zmiana to lokalna zmiana bez wpływu na zachowanie projektu, na przykład dopisanie komentarza, poprawa literówki albo dopisanie nowego akapitu lub punktu do pliku `.md`.

3. Większej ostrożności wymaga zmiana, która modyfikuje istniejącą treść albo wpływa na działanie projektu, na przykład zmiana kodu, usunięcie, przeniesienie albo zmiana nazwy pliku.

4. Jeśli zmiana jest większa albo niejednoznaczna, zatrzymaj się przed edycją, pokaż plan z listą plików i opisem zmian, a następnie poczekaj na wyraźną akceptację użytkownika. Jest to krytyczne gdy modyfikujesz więcej plików.

5. Jeśli zmienisz coś bez właściwej zgody, od razu się przyznaj, wypisz dokładnie co zostało zmienione i zaproponuj cofnięcie zmian bez wykonywania go automatycznie.

## Linkowanie

1. Jeśli wskazujesz w czacie konkretne miejsce w pliku, używaj linku workspace z hashem linii w formacie `[coding-rule.md](/p:/ai/components/common/coding-rule.md#L31)`.

2. Jeśli planujesz podać link do pliku, którego nazwa zawiera spację, najpierw jasno uprzedź użytkownika, że taki link może nie działać poprawnie, ponieważ linkowanie do plików ze spacjami jest niestabilne.

## Struktura odpowiedzi

1. Jeśli wypunktowujesz elementy odpowiedzi, preferuj listy numerowane zamiast nienumerycznych, tak aby użytkownik mógł łatwo wskazać, do którego punktu się odnosi.

## Pliki .md

Jeśli tworzysz plik `.md`, pamiętaj, że nie powinien on być w formacie utf8 i nie zawierać BOM.

# karpaty v.1

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

Tradeoff: These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## Think Before Coding

Don't assume. Don't hide confusion. Surface tradeoffs.

Before implementing:

1. State your assumptions explicitly. If uncertain, ask.
2. If multiple interpretations exist, present them - don't pick silently.
3. If a simpler approach exists, say so. Push back when warranted.
4. If something is unclear, stop. Name what's confusing. Ask.

## Simplicity First

Minimum code that solves the problem. Nothing speculative.

1. No features beyond what was asked.
2. No abstractions for single-use code.
3. No "flexibility" or "configurability" that wasn't requested.
4. No error handling for impossible scenarios.
5. If you write 200 lines and it could be 50, rewrite it.
6. Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## Surgical Changes

Touch only what you must. Clean up only your own mess.

When editing existing code:

1. Don't "improve" adjacent code, comments, or formatting.
2. Don't refactor things that aren't broken.
3. Match existing style, even if you'd do it differently.
4. If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:

1. Remove imports/variables/functions that YOUR changes made unused.
2. Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## Goal-Driven Execution

Define success criteria. Loop until verified.

Transform tasks into verifiable goals:

1. "Add validation" -> "Write tests for invalid inputs, then make them pass"
2. "Fix the bug" -> "Write a test that reproduces it, then make it pass"
3. "Refactor X" -> "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

1. [Step] -> verify: [check]
2. [Step] -> verify: [check]
3. [Step] -> verify: [check]

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

These guidelines are working if: fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

# branch-multisession v.6

1. Zanim zmienisz jakikolwiek plik, zawsze wykonaj  `branch step-begin "<name>"`.  Jeśli nie znasz `name`  wywołaj `branch get-name` - otrzymasz wtedy imie agenta (identyfikator sesji), zapamiętaj go.

2. Po zakończeniu zmian w repo zawsze wykonaj `branch step-end "<name>" "<desc>"`, używając tego samego imienia <name>.

3. Opis przekazywany do `step-end` jest po polsku opisuje dokonane zmiany.

4. Po udanym `step-begin` wypisz `STEP-BEGIN <name>`. Po udanym `step-end` wypisz `STEP-END: <opis>`.

5. Jeśli `branch` zwróci błąd albo instrukcję dalszego działania, przekaż ją użytkownikowi i postępuj zgodnie z nią.

# coding v.2

## Język kodowania

1. Kod źródłowy pisz po angielsku. Dotyczy to nazw klas, metod, zmiennych, stałych, parametrów, plików i innych elementów kodu.
2. Komentarze w kodzie pisz po polsku.
3. Jeśli tworzysz nowy plik, używaj nazwy w formacie `kebab-case`, o ile lokalne zasady projektu nie wymagają innego wzorca.

## Komentarze

1. Każda nowo utworzona metoda powinna mieć komentarz. Dla metod publicznych komentarz powinien być bardziej opisowy, a dla metod prywatnych może być krótszy i bardziej roboczy.

2. Jeśli środowisko wspiera system dokumentowania dla metod publicznych, używaj go. Na przykład w C# preferuj komentarze XML.

3. Jeśli komentarz używa tagów dokumentacyjnych, zapisuj je w możliwie zwartej formie, bez zbędnego rozbijania na wiele linii.
Preferuj zapis w jednej linii:
   /// <summary>Pobiera wewnętrzną encję z PasswordHash — tylko do auth i zmiany hasła</summary>
Nie zapisuj tego w postaci:
   /// <summary>
   /// Pobiera wewnętrzną encję z PasswordHash — tylko do auth i zmiany hasła
   /// </summary>

4. Komentarz ma wyjaśniać sens, intencję albo ważny kontekst, a nie opisywać oczywistości widocznych w kodzie.

5. Jeśli kod się zmienia, zaktualizuj też powiązany komentarz.

## Edycja

1. Przy edycji kodu preferuj małe, lokalne i celowe zmiany. Nie porządkuj przy okazji rzeczy, które nie są potrzebne do wykonania bieżącego zadania.

2. Zachowuj istniejący styl projektu, nazewnictwo i strukturę plików, jeśli nie ma wyraźnego powodu, by je zmienić.

## Testy

Jeśli tworzysz klasę, metodę albo logikę, która jest złożona, nieoczywista albo podatna na błąd, zaproponuj użytkownikowi dodanie testu.
