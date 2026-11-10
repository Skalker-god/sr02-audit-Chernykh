# Інвентаризація успадкованого коду

Джерело А: `V18-PickupPoint.zip`. Код скопійовано без змін у коміт `baseline`
(тег `baseline`). Проєкт — консольний застосунок .NET 8, простір імен
`PickupPoint`.

## Методика підрахунку

- **Фізичні рядки** — усі рядки файлу, включно з порожніми й коментарями.
- **Логічні рядки** — рядки з кодом; не рахуються порожні рядки, рядки лише з
  `//`-коментарем, рядки лише з `{`, `}` або `};`.
- **LOC методу** — логічні рядки тіла без сигнатури. Для методів із тілом-виразом
  (`=>`) LOC = 1, якщо вираз записано в одному логічному операторі.
- **CC** — 1 + кожен `if`, `for`, `foreach`, `while`, `case`, `catch`, `&&`,
  `||`, `??` і тернарний оператор `?:`.
- **Вкладеність** — максимальна глибина блоків у тілі (тіло методу = 1).
- **Параметри** — кількість параметрів у сигнатурі.
- **Fan-out** — кількість різних типів проєкту, згаданих у класі (поля,
  сигнатури, тіла методів). Типи BCL (`List`, `DateTime`, `Console`, `Math`)
  не рахуються.

Метрики знято скриптом `tools/Measure-Metrics.ps1`, повний результат —
`docs/metrics-before.csv`.

## Файли, типи та обсяг

| Файл | Типи | Призначення | Методів | Публічних | Логічних рядків | Фізичних рядків |
|---|---|---|---:|---:|---:|---:|
| `CashBox.cs` | `CashBox` | Каса: приймання, повернення, комісія | 3 | 3 | 21 | 37 |
| `DeskJournal.cs` | `DeskJournal` | Журнал операцій у тимчасовий файл | 1 | 1 | 11 | 20 |
| `OrderStore.cs` | `OrderStore` | Сховище замовлень, пошук за кодом, номером, телефоном | 4 | 4 | 41 | 61 |
| `PickupDesk.cs` | `PickupDesk` | Видача, повернення, частковий викуп | 5 | 4 | 92 | 139 |
| `PickupOrder.cs` | `OrderStatus`, `OrderItem`, `PickupOrder` | Стан, позиція, замовлення | 3 | 3 | 32 | 48 |
| `Program.cs` | `Program` | Демонстраційний сценарій зміни | 5 | 5 | 85 | 103 |
| `ShiftReport.cs` | `ShiftReport` | Звіт за полицями і закриття зміни | 2 | 2 | 31 | 46 |
| `StorageService.cs` | `StorageService` | Приймання, комірки, стани, нічне списання | 6 | 6 | 106 | 145 |
| **Разом** | **10 типів** | | **29** | **28** | **419** | **599** |

Єдиний непублічний метод — `PickupDesk.OldFee` (`private`). Усі інші методи,
включно з конструкторами, публічні.

## Усі методи

| № | Клас.Метод | Сигнатура | Видимість | LOC | CC | Тіло-вираз |
|---:|---|---|---|---:|---:|---|
| 1 | `CashBox.Take` | `void Take(decimal, string)` | public | 3 | 1 | ні |
| 2 | `CashBox.Refund` | `bool Refund(decimal, string)` | public | 7 | 2 | ні |
| 3 | `CashBox.Commission` | `decimal Commission(PickupOrder, decimal)` | public | 3 | 2 | ні |
| 4 | `DeskJournal.Write` | `static void Write(string)` | public | 7 | 3 | ні |
| 5 | `OrderStore.Add` | `PickupOrder Add(PickupOrder)` | public | 5 | 1 | ні |
| 6 | `OrderStore.FindByCode` | `PickupOrder FindByCode(string)` | public | 6 | 4 | ні |
| 7 | `OrderStore.FindByNumber` | `PickupOrder FindByNumber(string)` | public | 4 | 3 | ні |
| 8 | `OrderStore.FindByPhone` | `List<PickupOrder> FindByPhone(string, bool)` | public | 18 | 10 | ні |
| 9 | `PickupDesk.PickupDesk` | `PickupDesk()` | public | 0 | 1 | ні |
| 10 | `PickupDesk.PickupDesk` | `PickupDesk(OrderStore)` | public | 2 | 1 | ні |
| 11 | `PickupDesk.RefuseItem` | `bool RefuseItem(string, string)` | public | 8 | 4 | ні |
| 12 | `PickupDesk.Handout` | `string Handout(string, string, bool, bool, bool)` | public | 68 | 21 | ні |
| 13 | `PickupDesk.OldFee` | `decimal OldFee(int)` | private | 1 | 2 | ні |
| 14 | `OrderItem.OrderItem` | `OrderItem(string, decimal)` | public | 2 | 1 | ні |
| 15 | `PickupOrder.IsCod` | `bool IsCod()` | public | 1 | 1 | **так** (`=> CodAmount > 0`) |
| 16 | `PickupOrder.Line` | `string Line()` | public | 2 | 2 | ні |
| 17 | `Program.Main` | `static void Main(string[])` | public | 14 | 1 | ні |
| 18 | `Program.Arrivals` | `static PickupOrder[] Arrivals(StorageService)` | public | 20 | 1 | ні |
| 19 | `Program.Arrive` | `static PickupOrder Arrive(...)` | public | 11 | 3 | ні |
| 20 | `Program.Try` | `static void Try(...)` | public | 3 | 1 | ні |
| 21 | `Program.Day` | `static void Day(PickupDesk, PickupOrder[])` | public | 28 | 1 | ні |
| 22 | `ShiftReport.PrintShelf` | `void PrintShelf()` | public | 12 | 5 | ні |
| 23 | `ShiftReport.CloseShift` | `void CloseShift()` | public | 13 | 7 | ні |
| 24 | `StorageService.StorageService` | `StorageService(OrderStore)` | public | 1 | 1 | ні |
| 25 | `StorageService.RegisterArrival` | `PickupOrder RegisterArrival(...)` | public | 28 | 11 | ні |
| 26 | `StorageService.AssignCell` | `string AssignCell(PickupOrder)` | public | 15 | 7 | ні |
| 27 | `StorageService.PlaceOnShelf` | `bool PlaceOnShelf(PickupOrder)` | public | 7 | 3 | ні |
| 28 | `StorageService.ChangeStatus` | `bool ChangeStatus(string, OrderStatus)` | public | 24 | 9 | ні |
| 29 | `StorageService.do_sweep` | `int do_sweep()` | public | 17 | 6 | ні |

`PickupOrder.IsCod` — єдиний метод із тілом-виразом; попередня спроба його
пропустила, тому в CSV було 28 методів. Тут — 29.

## Підсумки «до»

| Показник | Значення |
|---|---:|
| Методів | 29 |
| Максимальна CC | 21 (`PickupDesk.Handout`) |
| Середня CC | 3,97 |
| Найдовший метод, логічних рядків | 68 (`PickupDesk.Handout`) |
| Максимальна вкладеність | 4 (`PickupDesk.Handout`) |
| Максимум параметрів | 8 (`Program.Arrive`) |
| Максимальний Fan-out | 7 (`PickupDesk`, `Program`) |
| Клонів ≥6 рядків | 2 групи, 5 фрагментів |

Примітка: `StorageService.RegisterArrival` має CC = 11, а не 10, як у
попередній спробі, бо методика враховує `catch` у `try/catch` навколо
`size.Trim()`. Саме цей `catch` є окремим дефектом D-10.
