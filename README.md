# sr02-audit-Chernykh

Самостійна робота №2 «Аудит якості та рефакторинг успадкованого коду».
Варіант 18, проєкт `PickupPoint` (пункт видачі замовлень).

- Студент: Черних Назар Юрійович, група 471.
- Джерело А: `V18-PickupPoint.zip`.
- Профільний дефект: три булеві прапорці в `PickupDesk.Handout`.
- Обов'язкові рефакторинги: R19 Remove Flag Argument, R01 Extract Method.

## Структура

```
src/            успадкований код (baseline — без змін)
Legacy.Tests/   характеризаційні тести (xUnit, .NET 8, coverlet)
docs/           інвентаризація, метрики, дефекти, план, рішення, борг
tools/          відтворюване вимірювання метрик
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
| `baseline` | вихідний код до аудиту, без змін |
| `safety-net` | сітка характеризаційних тестів готова |
| `after-refactoring` | останній рефакторинг |
| `baseline-with-tests` | baseline + тести і шви; тести проходять на оригіналі |

Репозиторій локальний: `C:\Users\user\Desktop\vscode\тест\sr02-audit-Chernykh`.
Віддалений репозиторій не налаштований, push не виконувався.
