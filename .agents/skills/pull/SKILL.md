---
name: pull
description: Pobieraj najnowszą wersję repozytorium z GitHuba, gdy użytkownik mówi „pobierz najnowszą wersję z GitHuba”, „pull” albo prosi o przełączenie na najnowszy branch. Wykrywaj najnowszy zdalny branch, ostrzegaj o brudnym worktree i różnicy branchy, a następnie przełączaj repo po wyraźnym potwierdzeniu użytkownika.
version: 2
modified-date: 2026-08-07
metadata:
  source_component: "components/common/pull-cmd.md"
  source_version: 2
  source_modified_date: 2026-08-07
---

# Pull

Wykonaj ten workflow w bieżącym repozytorium Git.

1. Sprawdź, czy repozytorium ma zdalne `origin`. Jeśli nie ma, krótko wyjaśnij problem i zakończ.
2. Wykonaj `git fetch --prune origin`.
3. Ustal branch z `origin/*`, którego commit na czubku ma najnowszą datę commita. Pomiń techniczny wskaźnik `origin/HEAD`.
4. Sprawdź, czy worktree jest czysty. Jeśli nie jest, powiedz dokładnie:

   `Masz lokalne niezapisane zmiany — zrób commit, zanim pobierzesz nową wersję.`

   Nie wykonuj `stash`, checkoutu, pulla ani innych operacji zmieniających branch.
5. Porównaj bieżący branch z najnowszym branchem na `origin`.
   - Jeśli są różne, ostrzeż użytkownika, podając bieżący branch, najnowszy branch oraz lokalną datę i godzinę najnowszego zdalnego commita. Zapytaj: `Czy mam przełączyć repo na <branch> i pobrać jego aktualny stan?`
   - Do przełączenia przejdź tylko po wyraźnej zgodzie użytkownika.
6. Jeśli bieżący branch jest najnowszym branchem albo użytkownik potwierdził przełączenie, przełącz się na lokalny branch śledzący `origin/<branch>`. Jeżeli lokalny branch jeszcze nie istnieje, utwórz go jako tracking branch.
7. Zaktualizuj go tylko fast-forwardem. Nie twórz merge commita i nie wykonuj rebase'a podczas pulla.
8. Po udanym checkout i fast-forward uruchom `branch reconcile`, a następnie `branch status`. Program `branch` ma sam odtworzyć lokalny workflow z markera Git albo usunąć czysty, stary stan po powrocie na branch bazowy. Nie edytuj `.workai` ręcznie.
9. Potwierdź wynik: aktywny branch, hash, lokalną datę i godzinę commita oraz stan workflow pokazany przez `branch status`.

## Zasady bezpieczeństwa

- „Najnowszy” oznacza commit o najnowszej dacie na czubku dowolnego brancha `origin/*`, nie tylko bieżącego brancha ani `main`.
- Nie zgaduj, że różnica branchy jest zamierzona — zawsze pokaż ostrzeżenie i zaczekaj na decyzję.
- Gdy fast-forward nie jest możliwy albo Git zgłosi konflikt, przekaż komunikat i zatrzymaj się. Nie rozwiązuj konfliktu ani nie nadpisuj historii samodzielnie.
- Jeśli `branch reconcile` zatrzyma się z powodu brudnego albo niejednoznacznego stanu, przekaż jego komunikat i nie próbuj samodzielnie przepisywać `.workai`.
