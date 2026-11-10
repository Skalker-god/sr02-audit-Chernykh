# Реєстр дефектів проєктування

Оцінки: **Серйозність** 1–5, **Частота** 1–5 (у пріоритеті враховується не
більше 4), **Пріоритет = Серйозність × min(Частота, 4)**.

| ID | Місце | Дефект | Тип | С | Ч | П |
|---|---|---|---|---:|---:|---:|
| D-01 | `PickupDesk.cs:38-132` | Довгий метод `Handout` | Long Method | 5 | 1 | 5 |
| D-02 | `PickupDesk.cs:38`, `Program.cs:78-94` | Три булеві прапорці (профільний) | Flag Argument | 5 | 4 | **20** |
| D-03 | `PickupDesk.cs:92-98`, `ShiftReport.cs:17-23`, `StorageService.cs:131-137` | Дублювання формули пені | Duplicated Code | 4 | 3 | 12 |
| D-04 | 4 файли | Магічні числа | Magic Numbers | 3 | 4 | 12 |
| D-05 | `PickupDesk`, `StorageService`, `ShiftReport` | Одержимість примітивами | Primitive Obsession | 3 | 4 | 12 |
| D-06 | `PickupDesk.Handout`, `StorageService.RegisterArrival`, `Program.Arrive`, `Program.Try` | Довгий список параметрів | Long Parameter List | 4 | 4 | 16 |
| D-07 | `PickupDesk.cs:21-33` | Заздрість до чужих даних | Feature Envy | 2 | 1 | 2 |
| D-08 | `StorageService.cs:8` | Коментар, що бреше | Misleading Comment | 3 | 1 | 3 |
| D-09 | `PickupDesk.cs:125`, `PickupDesk.cs:134-138` | Мертвий код | Dead Code | 2 | 2 | 4 |
| D-10 | `DeskJournal.cs:18`, `StorageService.cs:41-42` | Проковтнутий виняток | Swallowed Exception | 4 | 2 | 8 |
| D-11 | `StorageService.cs:20-25,33-37,73` | Код помилки або `null` замість винятку | Error Code / Null | 3 | 3 | 9 |
| D-12 | `OrderStore.cs:7`, `CashBox.cs:6`, `PickupDesk.cs:7` | Публічний стан і колекція | Encapsulation | 3 | 1 | 3 |
| D-13 | `StorageService.cs:26-32`, `OrderStore.cs:43-49` | Дублювання нормалізації телефону | Duplicated Code | 4 | 2 | 8 |
| D-14 | `StorageService.cs:96-115` | `switch` замість таблиці переходів | Conditional Logic | 3 | 1 | 3 |

Усього: **14 дефектів, 12 типів**. Найвищий пріоритет — D-02 (20), профільний
дефект варіанта. Далі D-06 (16), D-03/D-04/D-05 (12).

---

## D-01. Довгий метод `Handout`

- **Місце:** `src/PickupDesk.cs:38-132`.
- **Дефект:** один метод виконує пошук, перевірку телефону, повернення,
  перевірку статусу, прострочення, паспорт, пеню, частковий викуп, картку,
  касу, історію та журнал.
- **Доказ:**

```csharp
public string Handout(string code, string phone, bool isReturn, bool skipCheck, bool partial)
{
    LastAmount = 0m;
    PickupOrder o = Store.FindByCode(code);
    if (o == null)
        return "NOT_FOUND";
    if (!skipCheck)
    {
        List<PickupOrder> mine = Store.FindByPhone(phone, isReturn);
        ...
```

- **Чим шкодить:** 21 шлях виконання, 68 логічних рядків; будь-яка зміна
  вимагає читання всього методу, а тести покривають лише окремі гілки.
- **Категорія впливу:** супроводжуваність, тестованість.
- **Серйозність:** 5. **Частота:** 1. **Пріоритет:** 5.
- **Гіпотеза виправлення:** R01 Extract Method — виділити `CheckPhone`,
  `ProcessReturn`, `ProcessHandout`, `ApplyPartial`, `CompleteHandout`.

## D-02. Три булеві прапорці (профільний дефект)

- **Місце:** `src/PickupDesk.cs:38`; виклики — `src/Program.cs:78-94`.
- **Дефект:** `isReturn`, `skipCheck`, `partial` у публічній сигнатурі.
  Виклик `Handout(code, phone, false, true, false)` не читається без
  документації; 2³ = 8 комбінацій, з них частина недопустима.
- **Доказ:**

```csharp
public string Handout(string code, string phone,
    bool isReturn, bool skipCheck, bool partial)

static void Try(PickupDesk desk, PickupOrder o, string what, string phone,
    bool isReturn, bool skipCheck, bool partial)
{
    string res = desk.Handout(o.Code, phone, isReturn, skipCheck, partial);
    Console.WriteLine(o.Number + " " + what + ": " + res + " "
        + desk.LastAmount.ToString("0.00"));
}
```

- **Чим шкодить:** сценарій видачі за довіреністю неможливо прочитати з
  виклику; новий прапорець множить кількість шляхів і тестів.
- **Категорія впливу:** читабельність, тестованість, ризик помилки.
- **Серйозність:** 5. **Частота:** 4 (сигнатура + три форми виклику).
  **Пріоритет:** 20.
- **Гіпотеза виправлення:** R19 Remove Flag Argument — чотири публічні методи
  `Handout`, `HandoutByProxy`, `HandoutPartially`, `ReturnOrder`.

## D-03. Дублювання формули пені

- **Місце:** `PickupDesk.cs:92-98`, `ShiftReport.cs:17-23`,
  `StorageService.cs:131-137`.
- **Дефект:** тариф 15/25, пільгові 5 днів і формула `(days - 5) * rate`
  скопійовані тричі.
- **Доказ:**

```csharp
int days = (DateTime.Today - o.ArrivedAt.Date).Days;
decimal rate = 15m;
if (o.Size == "L")
    rate = 25m;
decimal fee = 0m;
if (days > 5)
    fee = (days - 5) * rate;
```

- **Чим шкодить:** зміна тарифу в одному місці дає різні суми в касі, звіті та
  нічному списанні; це вже створює ризик розбіжності.
- **Категорія впливу:** коректність бізнес-правила, супроводжуваність.
- **Серйозність:** 4. **Частота:** 3. **Пріоритет:** 12.
- **Гіпотеза виправлення:** R09 Extract Class — `StorageFeeCalculator`;
  R10 Move Method для спільного використання.

## D-04. Магічні числа

- **Місце:** `PickupDesk.cs`, `ShiftReport.cs`, `StorageService.cs`,
  `CashBox.cs`.
- **Дефект:** 14, 5000, 15, 25, 5, 10000, 20, 7, 4, 40, 0.02, 20 без імен.
- **Доказ:**

```csharp
int d2 = (DateTime.Today - o.HandedOutAt.Date).Days;
if (d2 > 14)
    return "RETURN_PERIOD_OVER";
if (o.DeclaredValue >= 5000m && PresentedId == null)
    return "ID_REQUIRED";
decimal rate = 15m;
if (o.Size == "L")
    rate = 25m;
if (total > 10000m && DateTime.Now.Hour >= 20)
    return "CARD_ONLY";
```

- **Чим шкодить:** сенс числа відомий лише автору; зміна порогу вимагає
  пошуку по всьому коду; коментарі не є захистом.
- **Категорія впливу:** супроводжуваність, розуміння.
- **Серйозність:** 3. **Частота:** 4. **Пріоритет:** 12.
- **Гіпотеза виправлення:** R04 Replace Magic Number with Symbolic Constant.

## D-05. Одержимість примітивами

- **Місце:** `PickupOrder.Size`, `Phone`, `decimal`-суми, `string`-коди.
- **Дефект:** розмір — `string`, телефон — `string`, гроші — `decimal` без
  валюти, результат операції — `string`.
- **Доказ:**

```csharp
public string Size = "M";      // S, M, L
public string Phone;           // 0XXXXXXXXX
public decimal DeclaredValue;
public decimal CodAmount;
public decimal PaidAmount;
public decimal PaidFee;
...
decimal rate = 15m;
if (o.Size == "L")
    rate = 25m;
```

- **Чим шкодить:** `"l"`, `" L"`, `null` і `"L"` поводяться по-різному;
  грошові суми можна додати до негрошових без попередження.
- **Категорія впливу:** надійність, типова безпека.
- **Серйозність:** 3. **Частота:** 4. **Пріоритет:** 12.
- **Гіпотеза виправлення:** R12 Replace Type Code with Class (розмір) і
  Value Object для грошей — винесено в борг, бо змінює контракт.

## D-06. Довгий список параметрів

- **Місце:** `PickupDesk.Handout` (5), `StorageService.RegisterArrival` (7),
  `Program.Arrive` (8), `Program.Try` (7).
- **Дефект:** чотири методи мають 5–8 позиційних параметрів, частина з них —
  булеві.
- **Доказ:**

```csharp
public PickupOrder RegisterArrival(string name, string phone, string size,
    decimal declaredValue, decimal cod, bool allowPartial, List<OrderItem> items)

static PickupOrder Arrive(StorageService s, string name, string phone, string size,
    decimal cod, bool partial, int daysAgo, params OrderItem[] items)

static void Try(PickupDesk desk, PickupOrder o, string what, string phone,
    bool isReturn, bool skipCheck, bool partial)
```

- **Чим шкодить:** при виклику легко переплутати `declaredValue` і `cod`,
  `isReturn` і `skipCheck`; кожен новий параметр ламає всі виклики.
- **Категорія впливу:** читабельність, ризик помилки.
- **Серйозність:** 4. **Частота:** 4. **Пріоритет:** 16.
- **Гіпотеза виправлення:** R11 Introduce Parameter Object (`HandoutRequest`).

## D-07. Заздрість до чужих даних

- **Місце:** `src/PickupDesk.cs:21-33`.
- **Дефект:** `RefuseItem` сам перебирає `PickupOrder.Items` і змінює
  `OrderItem.Refused`, хоча це поведінка замовлення.
- **Доказ:**

```csharp
public bool RefuseItem(string code, string sku)
{
    PickupOrder o = Store.FindByCode(code);
    if (o == null)
        return false;
    foreach (OrderItem it in o.Items)
        if (it.Sku == sku)
        {
            it.Refused = true;
            return true;
        }
    return false;
}
```

- **Чим шкодить:** правило відмови розмазане між стійкою і замовленням; будь-яка
  зміна структури позицій ламає стійку.
- **Категорія впливу:** зв'язність, супроводжуваність.
- **Серйозність:** 2. **Частота:** 1. **Пріоритет:** 2.
- **Гіпотеза виправлення:** R10 Move Method — `PickupOrder.Refuse(sku)`.

## D-08. Коментар, що бреше

- **Місце:** `src/StorageService.cs:8`.
- **Дефект:** поле `StorageDays = 7` супроводжується коментарем «зберігаємо
  5 днів». Насправді безкоштовних днів 5, а строк зберігання — 7.
- **Доказ:**

```csharp
public int StorageDays = 7;   // зберігаємо 5 днів

...
o.StorageUntil = o.ArrivedAt.Date.AddDays(StorageDays);

...
if (days > 5)
    fee = (days - 5) * rate;
```

- **Чим шкодить:** читач вірить коментарю і змінює не те число; це вже
  призводить до плутанини між «строк зберігання» і «пільгові дні».
- **Категорія впливу:** розуміння, супроводжуваність.
- **Серйозність:** 3. **Частота:** 1. **Пріоритет:** 3.
- **Гіпотеза виправлення:** R04 — виправити лише коментар, значення 7 не
  змінювати.

## D-09. Мертвий код

- **Місце:** `src/PickupDesk.cs:125` (закоментований `SmsGateway.Send`),
  `src/PickupDesk.cs:134-138` (невикористаний `OldFee`).
- **Дефект:** код, який ніколи не виконується, але виглядає як робочий.
- **Доказ:**

```csharp
// SmsGateway.Send(o.Phone, "Thank you! Order " + o.Number);
Storage.BusyCells.Remove(o.Cell);
PresentedId = null;
HandoutsToday++;
LastAmount = total;
DeskJournal.Write("HANDOUT " + o.Number + " " + total);
return "OK";
...
private decimal OldFee(int days)
{
    return days > 7 ? (days - 7) * 10m : 0m;
}
```

- **Чим шкодить:** пошук посилань показує «використання», якого немає; старий
  тариф 10 грн вводить в оману.
- **Категорія впливу:** супроводжуваність.
- **Серйозність:** 2. **Частота:** 2. **Пріоритет:** 4.
- **Гіпотеза виправлення:** R17 Remove Dead Code.

## D-10. Проковтнутий виняток

- **Місце:** `src/DeskJournal.cs:18` (`catch { }`),
  `src/StorageService.cs:41-42` (`try { size.Trim() } catch { }`).
- **Дефект:** виняток зникає без сліду; помилка не потрапляє ні в журнал, ні
  в результат.
- **Доказ:**

```csharp
try
{
    string path = Path.Combine(Path.GetTempPath(), "pickup-point-journal.log");
    string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    File.AppendAllText(path, stamp + " " + text + "\n");
}
catch { }
...
try { o.Size = size.Trim().ToUpper(); }
catch (Exception) { }   // без розміру лишається M
```

- **Чим шкодить:** журнал може не писатися місяцями; `null`-розмір тихо стає
  `M`, і клієнт отримує неправильний тариф.
- **Категорія впливу:** надійність, діагностика.
- **Серйозність:** 4. **Частота:** 2. **Пріоритет:** 8.
- **Гіпотеза виправлення:** R18 — явна перевірка `size != null`; журнал —
  окремий борг TD-06 (порожній `catch` не можна прибрати без зміни поведінки).

## D-11. Код помилки або `null` замість винятку

- **Місце:** `src/StorageService.cs:20-25` (`return null`), `:33-37`
  (`return null`), `:73` (`return null`).
- **Дефект:** неможливо відрізнити «немає місць» від «некоректний телефон»;
  виклик зобов'язаний перевіряти `null`.
- **Доказ:**

```csharp
if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phone)
    || items == null || items.Count == 0)
{
    Console.WriteLine("  ! arrival rejected: empty field");
    return null;
}
...
if (p.Length != 10)
{
    Console.WriteLine("  ! arrival rejected: invalid phone " + phone);
    return null;
}
```

- **Чим шкодить:** кожен виклик має пам'ятати про `null`; причина відмови
  втрачається; у ланцюжку з'являється `NullReferenceException`.
- **Категорія впливу:** надійність, API.
- **Серйозність:** 3. **Частота:** 3. **Пріоритет:** 9.
- **Гіпотеза виправлення:** R20 Replace Error Code with Exception — винесено в
  борг, бо змінює публічний контракт і поведінку.

## D-12. Публічний стан і колекція

- **Місце:** `src/OrderStore.cs:7`, `src/CashBox.cs:6`,
  `src/PickupDesk.cs:7`.
- **Дефект:** `Orders` — публічний `List`, статичні `nextNo`, `Balance`,
  `HandoutsToday` змінюються ззовні.
- **Доказ:**

```csharp
public class OrderStore
{
    public static int nextNo = 1;
    public List<PickupOrder> Orders = new List<PickupOrder>();

    public PickupOrder Add(PickupOrder o)
    {
        o.Number = "PP-" + nextNo.ToString("000000");
        o.Code = (100000 + nextNo * 7919 % 900000).ToString();
        nextNo++;
        Orders.Add(o);
        return o;
    }
```

- **Чим шкодить:** інваріанти сховища й каси можна порушити з будь-якого місця;
  тести змушені вручну скидати статичний стан.
- **Категорія впливу:** інкапсуляція, тестованість.
- **Серйозність:** 3. **Частота:** 1. **Пріоритет:** 3.
- **Гіпотеза виправлення:** R13 Encapsulate Collection; статичний стан —
  борг TD-04.

## D-13. Дублювання нормалізації телефону

- **Місце:** `src/StorageService.cs:26-32`, `src/OrderStore.cs:43-49`.
- **Дефект:** 7 логічних рядків нормалізації скопійовані в прийманні та пошуку.
- **Доказ:**

```csharp
string p = phone.Trim();
p = p.Replace(" ", "").Replace("-", "");
p = p.Replace("(", "").Replace(")", "");
if (p.StartsWith("+38"))
    p = p.Substring(3);
if (p.Length == 12 && p.StartsWith("38"))
    p = p.Substring(2);
```

- **Чим шкодить:** якщо в одній копії додати новий формат, пошук перестане
  знаходити замовлення; саме так виникають «невірний телефон» на рівному місці.
- **Категорія впливу:** коректність, супроводжуваність.
- **Серйозність:** 4. **Частота:** 2. **Пріоритет:** 8.
- **Гіпотеза виправлення:** R01/R09 — `PhoneNormalizer.Normalize`.

## D-14. `switch` замість таблиці переходів

- **Місце:** `src/StorageService.cs:96-115`.
- **Дефект:** допустимі переходи станів закодовані `switch` із `bool ok`;
  таблиця переходів відсутня.
- **Доказ:**

```csharp
bool ok = false;
switch (o.Status)
{
    case OrderStatus.Arrived:
        ok = to == OrderStatus.Stored;
        break;
    case OrderStatus.Stored:
        ok = to == OrderStatus.HandedOut || to == OrderStatus.Returned
            || to == OrderStatus.Expired;
        break;
    case OrderStatus.HandedOut:
    case OrderStatus.Expired:
        ok = to == OrderStatus.Returned;
        break;
}
```

- **Чим шкодить:** додавання стану вимагає правки `switch`; `default` відсутній,
  тому новий стан мовчки забороняє всі переходи.
- **Категорія впливу:** супроводжуваність, коректність.
- **Серйозність:** 3. **Частота:** 1. **Пріоритет:** 3.
- **Гіпотеза виправлення:** R08 Replace Conditional with Table — словник
  `OrderStatus -> допустимі стани`.
