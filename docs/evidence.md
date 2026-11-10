# Докази

## 1. Тести змінювалися лише в місцях виклику

Команда:

```cmd
git log --oneline safety-net..after-refactoring -- Legacy.Tests
```

Вивід (`docs/evidence-git-log-tests.txt`):

```
be25ffa refactor(StorageService): R02 перейменовано do_sweep і Program.Try
362cc2c refactor(PickupDesk): R19 прибрано булеві прапорці
```

Обидва коміти змінюють у тестах лише виклики: у `362cc2c` —
`Handout(code, phone, false, false, false)` → `Handout(code, phone)` тощо;
у `be25ffa` — `do_sweep()` → `SweepExpired()`. Жодне очікуване значення
(`Assert.Equal`) не змінено.

## 2. Однакова кількість тестів на baseline і HEAD

| Стан | Файл | Результат |
|---|---|---|
| Гілка `baseline-with-tests` (`e17c8c3`) | `docs/test-baseline-with-tests.txt` | `Passed: 38, Failed: 0` |
| HEAD (`6389576` + docs) | `docs/test-head.txt` | `Passed: 38, Failed: 0` |

Обидва прогони — `dotnet test Legacy.Tests\Legacy.Tests.csproj -c Release`.
На baseline тести проходять на оригінальному 5-аргументному API; на HEAD — на
новому, після R19/R02.

## 3. Незмінний вивід програми

```cmd
src\bin\Release\net8.0\Legacy.exe > docs\out-before.txt   (baseline)
src\bin\Release\net8.0\Legacy.exe > docs\out-after.txt    (HEAD)
git diff --no-index docs\out-before.txt docs\out-after.txt
```

Результат: `exit code 0`, вивід порожній. Обидва файли — 47 рядків,
1634 байти, UTF-8 без BOM. Повний вміст наведено в розділі 5 звіту.

## 4. Покриття

| Прогін | Файл | `PickupDesk` рядки | Проєкт рядки |
|---|---|---:|---:|
| До рефакторингу | `docs/coverage-run.txt` | 97,7 % | 71,6 % |
| Після рефакторингу | `docs/coverage-run-after.txt` | 99,0 % | 74,0 % |

## 5. Мутаційна перевірка

- `docs/mutation-red.txt` — `rate = 15m` → `16m`: `Failed: 1`,
  `Expected: 15, Actual: 16`.
- `docs/mutation-green.txt` — після відкату: `Passed: 1, Failed: 0`.
- Повний прогін після відкату: `Passed: 38, Failed: 0`.

## 6. Клони

`docs/clones-before.txt` — вивід `Find-Clones.ps1` на baseline: 4 вікна
(2 групи). `docs/clones-after.txt` — на HEAD: `clones=0`.
