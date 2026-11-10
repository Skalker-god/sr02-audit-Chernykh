namespace PickupPoint;

// Прийом від перевізника, комірки, нічне списання
public class StorageService
{
    public const decimal SafeCellThreshold = 5000m;
    public const int SafeCellCount = 4;
    public const int RegularCellCount = 40;
    public static bool NotifyBySms = false;   // шлюз вимкнули
    public OrderStore Store;
    public const int StorageDays = 7;   // 7 днів на полиці, з них 5 безкоштовні
    public decimal SenderFees;    // платить магазин
    public List<string> BusyCells = new List<string>();
    private readonly StorageFeeCalculator _fees = new StorageFeeCalculator();

    public StorageService(OrderStore store)
    {
        Store = store;
    }

    public PickupOrder RegisterArrival(string name, string phone, string size,
        decimal declaredValue, decimal cod, bool allowPartial, List<OrderItem> items)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phone)
            || items == null || items.Count == 0)
        {
            Console.WriteLine("  ! arrival rejected: empty field");
            return null;
        }
        string p = PhoneNormalizer.Normalize(phone);
        if (p.Length != 10)
        {
            Console.WriteLine("  ! arrival rejected: invalid phone " + phone);
            return null;
        }
        PickupOrder o = new PickupOrder();
        o.RecipientName = name;
        o.Phone = p;
        // R18: явна перевірка замість try/catch; null лишає типовий розмір M
        if (size != null)
            o.Size = size.Trim().ToUpper();
        o.DeclaredValue = declaredValue;
        o.CodAmount = cod;   // TODO: а якщо мінус?
        o.AllowPartial = allowPartial;
        o.Items = items;
        o.ArrivedAt = DateTime.Now;
        o.History.Add("Arrived");
        Store.Add(o);
        DeskJournal.Write("ARRIVED " + o.Number);
        return o;
    }

    // цінне - в сейф, великогабарит - на підлогу
    public string AssignCell(PickupOrder o)
    {
        string prefix = PickupPrefix(o);
        int max = prefix == "SAFE-" ? SafeCellCount : RegularCellCount;
        for (int i = 1; i <= max; i++)
        {
            string cell = prefix + i.ToString("00");
            if (BusyCells.Contains(cell))
                continue;
            BusyCells.Add(cell);
            return cell;
        }
        return null;   // місць нема
    }

    // R06: замість ланцюжка else-if — ранні повернення.
    private static string PickupPrefix(PickupOrder o)
    {
        if (o.DeclaredValue >= SafeCellThreshold)
            return "SAFE-";
        if (o.Size == "L")
            return "F-";
        if (o.Size == "S")
            return "A-";
        return "B-";
    }

    public bool PlaceOnShelf(PickupOrder o)
    {
        if (!ChangeStatus(o.Number, OrderStatus.Stored))
            return false;
        o.Cell = AssignCell(o);
        o.StorageUntil = o.ArrivedAt.Date.AddDays(StorageDays);
        if (NotifyBySms)
            Console.WriteLine("SMS to " + o.Phone + ": code " + o.Code);
        return true;
    }

    // Arrived -> Stored -> HandedOut / Returned / Expired
    public bool ChangeStatus(string number, OrderStatus to)
    {
        PickupOrder o = Store.FindByNumber(number);
        if (o == null)
        {
            Console.WriteLine("  ! order " + number + " not found");
            return false;
        }
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
            case OrderStatus.HandedOut:   // 14 днів перевіряє стійка
            case OrderStatus.Expired:     // назад продавцю
                ok = to == OrderStatus.Returned;
                break;
        }
        if (!ok)
        {
            Console.WriteLine("  ! cannot move " + o.Number + ": " + o.Status + " -> " + to);
            return false;
        }
        o.Status = to;
        o.History.Add(to.ToString());
        DeskJournal.Write("STATUS " + o.Number + " " + to);
        return true;
    }

    // nightly job, scheduler runs it at 03:00
    public int SweepExpired()
    {
        int cnt = 0;
        foreach (PickupOrder o in Store.Orders)
        {
            if (o.Status != OrderStatus.Stored || DateTime.Today <= o.StorageUntil)
                continue;
            o.Status = OrderStatus.Expired;
            SenderFees += _fees.Calculate(o, DateTime.Today);
            o.History.Add("Expired");
            BusyCells.Remove(o.Cell);
            cnt++;
        }
        return cnt;
    }
}
