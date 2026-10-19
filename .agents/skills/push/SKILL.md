---
name: push
description: Zapisuj i wypychaj na GitHuba cały bieżący stan repozytorium, gdy użytkownik mówi „push”, „wypchnij wszystko na GitHuba” albo chce czysty worktree. Uwzględniaj wszystkie nieignorowane zmiany, także wykonane przez użytkownika i inne agenty, korzystaj z workflow branch, gdy jest aktywny, oraz weryfikuj synchronizację po pushu.
version: 2
modified-date: 2026-08-07
metadata:
  source_component: "components/common/push-cmd.md"
  source_version: 2
  source_modified_date: 2026-08-07
---

# Push

Wykonaj ten workflow w bieżącym repozytorium Git.

1. Sprawdź, czy repozytorium ma zdalne `origin`. Jeśli nie ma, krótko wyjaśnij problem i zakończ.
2. Uruchom `branch reconcile`, a następnie `branch status`. Nie rozpoznawaj aktywnego workflow wyłącznie po obecności `.workai\branch-state.json`.
   - Jeśli `branch status` pokazuje aktywny workflow, wykonaj `branch flush`, aby zapisać wszystkie zmiany i użyć opisu z workflow brancha.
   - Jeśli otwarte kroki agentów blokują zwykły flush, pokaż krótkie ostrzeżenie z ich nazwami i wykonaj `branch flush --force`. Jawne polecenie użytkownika „push” oznacza zgodę na zapis wszystkich zmian bieżącego brancha.
   - Po udanym flush uruchom ponownie `branch reconcile`. Dzięki temu starszy lokalny workflow bez markera zostanie zmigrowany do przenośnego formatu, gdy worktree jest już czysty.
   - Nie edytuj ręcznie pliku `.workai\branch-state.json`.
3. Jeśli workflow branch nie jest aktywny, dodaj wszystkie nieignorowane zmiany przez `git add -A` i utwórz jeden commit. Użyj krótkiego opisu rzeczywistych zmian; nie pomijaj zmian wykonanych przez użytkownika ani inne agenty.
4. Wykonaj `git fetch origin` i sprawdź, czy zdalny odpowiednik bieżącego brancha ma commity, których nie ma lokalnie.
   - Jeśli tak, ostrzeż użytkownika, że na GitHubie są nowsze commity na tym samym branchu.
   - Następnie wykonaj rebase lokalnych commitów na aktualnym zdalnym branchu. Jeżeli powstanie konflikt, przekaż komunikat i zatrzymaj się; nie rozwiązuj go samodzielnie.
5. Wypchnij bieżący branch na `origin`. Jeżeli nie ma jeszcze powiązania zdalnego, ustaw je podczas pierwszego pushu.
6. Nie używaj force-pusha ani nie nadpisuj historii zdalnej.
7. Zweryfikuj, że worktree jest czysty, a bieżący branch nie jest ani do przodu, ani do tyłu względem swojego zdalnego odpowiednika. Potwierdź branch i hash wypchniętego commita.

## Zasady bezpieczeństwa

- „Wszystko” oznacza wszystkie nieignorowane zmiany widoczne dla Gita w bieżącym repozytorium.
- Gdy nie ma zmian do commita, nie twórz pustego commita; przejdź od razu do synchronizacji i pushu.
- Rebase nie zmienia treści zmian celowo: tylko umieszcza lokalne commity po commitach, które wcześniej pojawiły się na tym samym branchu na GitHubie.
