# Покриття і чутливість сітки тестів

## Склад сітки

- Проєкт: `Legacy.Tests` (xUnit 2.9.3, .NET 8, coverlet 6.0.4).
- Тестів: **38** (35 фактів + 3 випадки `[Theory]` для форматів телефону).
- Імена: `Метод_Умова_Результат`; структура AAA.
- Статичний стан скидається в `NewDesk`: `OrderStore.nextNo`, `CashBox.Balance`,
  `PickupDesk.HandoutsToday`, `DeskJournal.Enabled`, `StorageService.NotifyBySms`.
- Шви: `PickupDesk.Clock` (`Func<DateTime>`) і `PickupDesk.Log`
  (`Action<string>`), додані окремими комітами `test:`.

## Покриття (baseline + шари, до рефакторингу)

Команда:

```cmd
dotnet test Legacy.Tests\Legacy.Tests.csproj -c Release ^
  --collect:"XPlat Code Coverage"
```

| Клас | Рядки, % | Гілки, % |
|---|---:|---:|
| `PickupDesk` (донор) | **97,7** | **95,8** |
| `StorageService` | 94,4 | 85,7 |
| `OrderStore` | 97,1 | 96,4 |
| `ShiftReport` | 84,6 | 70,0 |
| `CashBox` | 85,7 | 75,0 |
| `DeskJournal` | 33,3 | 50,0 |
| `PickupOrder` | 100,0 | 50,0 |
| `OrderItem` | 100,0 | 100,0 |
| `Program` | 0,0 | 0,0 |
| **Разом** | **71,6** | **85,4** |

`PickupDesk` — 67 із 69 рядків, 23 із 24 гілок. Непокриті: `OldFee` (мертвий
код) і одна гілка `PresentedId`. `Program` не тестується, бо це демонстраційний
`Main`; прогалина внесена в борг TD-05. `DeskJournal` покритий лише раннім
виходом `Enabled = false`; файловий запис — борг TD-06.

## Мутаційна перевірка

Тимчасово змінено тариф у `PickupDesk.Handout`: `decimal rate = 15m` →
`decimal rate = 16m`. Запущено лише тест
`Handout_StorageSixDays_AddsFee`.

До мутації: `Passed: 38, Failed: 0`.
Після мутації: `Failed: 1, Passed: 0` —

```
Assert.Equal() Failure: Values differ
Expected: 15
Actual:   16
```

Після відкату: `Passed: 1, Failed: 0`, повний прогін — `Passed: 38`.
Логи: `docs/mutation-red.txt`, `docs/mutation-green.txt`.

## Межові випадки, зафіксовані тестами

| Межа | Тест | Очікування |
|---|---|---|
| 5 днів зберігання | `Handout_StorageExactlyFiveDays_NoFee` | пеня 0 |
| 6 днів | `Handout_StorageSixDays_AddsFee` | пеня 15 |
| 14 днів повернення | `Handout_ReturnOnFourteenthDay_Refunds` | `RETURNED` |
| 15 днів | `Handout_ReturnOnFifteenthDay_ReturnsPeriodOver` | `RETURN_PERIOD_OVER` |
| `DeclaredValue` 4999 | `Handout_DeclaredValue4999WithoutId_ReturnsOk` | `OK` |
| `DeclaredValue` 5000 | `Handout_DeclaredValue5000WithoutId_ReturnsIdRequired` | `ID_REQUIRED` |
| сума 10000,00 | `Handout_TotalExactly10000At20_ReturnsOk` | `OK` |
| сума 10000,01 | `Handout_Total10000Point01At20_ReturnsCardOnly` | `CARD_ONLY` |
| 19:00 | `Handout_TotalOver10000At19_ReturnsOk` | `OK` |
| 20:00 | `Handout_TotalOver10000At20_ReturnsCardOnly` | `CARD_ONLY` |
| `code = null` | `Handout_NullCode_ReturnsNotFound` | `NOT_FOUND` |
| порожні `Items` | `Handout_PartialEmptyItems_ReturnsOk` | `Partial 0/0` |
| від'ємний COD | `Handout_NegativeCodAmount_Characterization` | `OK`, сума −100 |
| формати телефону | `Handout_PhoneFormat_ReturnsOk` (3 випадки) | `OK` |
