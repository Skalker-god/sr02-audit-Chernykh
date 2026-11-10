namespace PickupPoint;

// Прийом від перевізника, комірки, нічне списання
public class StorageService
{
    public static bool NotifyBySms = false;   // шлюз вимкнули
    public OrderStore Store;
    public int StorageDays = 7;   // зберігаємо 5 днів
    public decimal SenderFees;    // платить магазин
    public List<string> BusyCells = new List<string>();

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
        string p = phone.Trim();
        p = p.Replace(" ", "").Replace("-", "");
        p = p.Replace("(", "").Replace(")", "");
        if (p.StartsWith("+38"))
            p = p.Substring(3);
        if (p.Length == 12 && p.StartsWith("38"))
            p = p.Substring(2);
        if (p.Length != 10)
        {
            Console.WriteLine("  ! arrival rejected: invalid phone " + phone);
            return null;
        }
        PickupOrder o = new PickupOrder();
        o.RecipientName = name;
        o.Phone = p;
        try { o.Size = size.Trim().ToUpper(); }
        catch (Exception) { }   // без розміру лишається M
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
        string prefix = "B-";
        if (o.DeclaredValue >= 5000m)
            prefix = "SAFE-";
        else if (o.Size == "L")
            prefix = "F-";
        else if (o.Size == "S")
            prefix = "A-";
        int max = prefix == "SAFE-" ? 4 : 40;
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
    public int do_sweep()
    {
        int cnt = 0;
        foreach (PickupOrder o in Store.Orders)
        {
            if (o.Status != OrderStatus.Stored || DateTime.Today <= o.StorageUntil)
                continue;
            o.Status = OrderStatus.Expired;
            int days = (DateTime.Today - o.ArrivedAt.Date).Days;
            decimal rate = 15m;
            if (o.Size == "L")
                rate = 25m;
            decimal fee = 0m;
            if (days > 5)
                fee = (days - 5) * rate;
            SenderFees += fee;
            o.History.Add("Expired");
            BusyCells.Remove(o.Cell);
            cnt++;
        }
        return cnt;
    }
}
