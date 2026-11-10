# Клони ≥6 рядків: до і після

Клон фіксується, якщо два фрагменти збігаються після нормалізації (однакові
оператори, інші лише імена змінних/літерали) і мають ≥6 логічних рядків.

## До рефакторингу

### Клон 1 — нормалізація телефону (7 логічних рядків)

Фрагмент A: `src/StorageService.cs:26-32`

```csharp
string p = phone.Trim();
p = p.Replace(" ", "").Replace("-", "");
p = p.Replace("(", "").Replace(")", "");
if (p.StartsWith("+38"))
    p = p.Substring(3);
if (p.Length == 12 && p.StartsWith("38"))
    p = p.Substring(2);
```

Фрагмент B: `src/OrderStore.cs:43-49`

```csharp
string p = phone.Trim();
p = p.Replace(" ", "").Replace("-", "");
p = p.Replace("(", "").Replace(")", "");
if (p.StartsWith("+38"))
    p = p.Substring(3);
if (p.Length == 12 && p.StartsWith("38"))
    p = p.Substring(2);
```

Наслідок: будь-яка зміна правила нормалізації має вноситися двічі; розбіжність
між копіями дає різні результати пошуку й приймання.

### Клон 2 — формула пені (7 логічних рядків, 3 фрагменти)

Фрагмент A: `src/PickupDesk.cs:92-98`

```csharp
int days = (DateTime.Today - o.ArrivedAt.Date).Days;
decimal rate = 15m;
if (o.Size == "L")
    rate = 25m;
decimal fee = 0m;
if (days > 5)
    fee = (days - 5) * rate;
```

Фрагмент B: `src/ShiftReport.cs:17-23` — той самий код у `PrintShelf`.

Фрагмент C: `src/StorageService.cs:131-137` — той самий код у `do_sweep`.

Наслідок: тариф і пільгові 5 днів задані в трьох місцях; зміна бізнес-правила
потребує синхронної правки трьох копій.

## Після рефакторингу

| Клон | Було | Стало | Чим усунено |
|---|---|---|---|
| Нормалізація телефону | 2 фрагменти, 7 рядків | 0 | `PhoneNormalizer.Normalize` (R01/R09) |
| Формула пені | 3 фрагменти, 7 рядків | 0 | `StorageFeeCalculator.Calculate` (R09) |

Перевірка після: повторний пошук клонів ≥6 рядків у `src/` не знаходить жодної
групи. Див. `docs/metrics-after.csv` і розділ 6 звіту.
