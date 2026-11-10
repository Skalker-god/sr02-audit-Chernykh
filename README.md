# sr02-audit-Chernykh

Самостійна робота №2 «Аудит якості та рефакторинг успадкованого коду».
Варіант 18, проєкт `PickupPoint` (пункт видачі замовлень).

- Студент: Черних Назар Юрійович, група 471.
- Джерело А: `V18-PickupPoint.zip`.
- Профільний дефект: три булеві прапорці в `PickupDesk.Handout`.
- Обов'язкові рефакторинги: R19 Remove Flag Argument, R01 Extract Method.

## Структура

```
src/            успадкований код (baseline — без змін) і результати рефакторингу
Legacy.Tests/   характеризаційні тести (xUnit, .NET 8, coverlet)
docs/           інвентаризація, метрики, дефекти, план, рішення, борг, докази
tools/          відтворюване вимірювання метрик, пошук клонів, рисунки
```

## Збірка й тести

```cmd
dotnet build Legacy.sln -c Release
dotnet test Legacy.Tests\Legacy.Tests.csproj -c Release
dotnet test Legacy.Tests\Legacy.Tests.csproj -c Release --collect:"XPlat Code Coverage"
```

## Стани репозиторію

| Тег / гілка | Призначення |
|---|---|
| `baseline` | вихідний код до аудиту, без змін (`c58fc5f`) |
| `safety-net` | сітка характеризаційних тестів готова (`bfef8e3`) |
| `after-refactoring` | останній рефакторинг (`6389576`) |
| `baseline-with-tests` | baseline + тести і шви; 38 тестів проходять (`e17c8c3`) |

## Підсумок

| Показник | До | Після |
|---|---:|---:|
| Методів | 29 | 47 |
| Максимальна CC | 21 | 7 |
| Середня CC | 3,93 | 2,60 |
| Найдовший метод, логічних рядків | 68 | 28 |
| Клонів ≥6 рядків | 2 групи | 0 |
| Тестів | — | 38, Failed 0 |
| Покриття `PickupDesk` | — | 99,0 % |

Деталі — `docs/inventory.md`, `docs/metrics-before.csv`,
`docs/metrics-after.csv`, `docs/metrics-after.md`, `docs/debt.md`,
`docs/decisions.md`.

## Як відтворити

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Measure-Metrics.ps1 -Src src -Out docs\metrics-after.csv
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Find-Clones.ps1 -Src src
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Make-Figure1.ps1 -Metrics docs\metrics-before.csv -Out docs\fig1-cc-before.png
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Make-Figure2.ps1 -Before docs\metrics-before.csv -After docs\metrics-after.csv -Out docs\fig2-cc-before-after.png
```
