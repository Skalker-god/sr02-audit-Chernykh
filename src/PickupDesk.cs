namespace PickupPoint;

// Стійка видачі: видача, повернення, частковий викуп
public class PickupDesk
{
    public static int HandoutsToday = 0;
    public OrderStore Store;
    public StorageService Storage;
    public CashBox Cash = new CashBox();
    public decimal LastAmount;   // сума останньої операції
    public string PresentedId;   // паспорт клієнта (для цінних)

    public PickupDesk() : this(new OrderStore()) { }

    public PickupDesk(OrderStore store)
    {
        Store = store;
        Storage = new StorageService(store);
    }

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

    // Видача або повернення. Повертає "OK" або код помилки.
    // isReturn - повернення, skipCheck - за довіреністю (без звірки
    // телефону), partial - частковий викуп (див. RefuseItem)
    public string Handout(string code, string phone, bool isReturn, bool skipCheck, bool partial)
    {
        LastAmount = 0m;
        PickupOrder o = Store.FindByCode(code);
        if (o == null)
            return "NOT_FOUND";
        if (!skipCheck)
        {
            List<PickupOrder> mine = Store.FindByPhone(phone, isReturn);
            if (!mine.Contains(o))
            {
                o.FailedAttempts++;   // TODO: блокувати після 3 спроб
                return "WRONG_PHONE";
            }
        }
        if (isReturn)
        {
            if (o.Status == OrderStatus.Stored)
            {
                // відмова на пункті, гроші ще не брали
                o.Status = OrderStatus.Returned;
                o.History.Add("Refused");
                return "REFUSED";
            }
            if (o.Status != OrderStatus.HandedOut)
                return "ERR_STATUS " + o.Status;
            int d2 = (DateTime.Today - o.HandedOutAt.Date).Days;   // днів з видачі
            if (d2 > 14)
                return "RETURN_PERIOD_OVER";
            decimal back = o.PaidAmount - o.PaidFee;   // зберігання не вертаємо
            Cash.Refund(back, o.Number);
            o.Status = OrderStatus.Returned;
            o.History.Add("Returned");
            LastAmount = back;
            return "RETURNED";
        }
        if (o.Status != OrderStatus.Stored)
        {
            // повернене чи протерміноване не видаємо
            Console.WriteLine("  ! " + o.Number + " is " + o.Status + ", cannot hand out");
            return "ERR_STATUS " + o.Status;
        }
        if (!skipCheck)
        {
            if (DateTime.Today > o.StorageUntil)
            {
                o.Status = OrderStatus.Expired;
                o.History.Add("Expired");
                Console.WriteLine("  ! storage term of " + o.Number + " is over");
                return "EXPIRED";
            }
        }
        if (o.DeclaredValue >= 5000m && PresentedId == null)
            return "ID_REQUIRED";
        int days = (DateTime.Today - o.ArrivedAt.Date).Days;
        decimal rate = 15m;
        if (o.Size == "L")
            rate = 25m;
        decimal fee = 0m;
        if (days > 5)
            fee = (days - 5) * rate;
        decimal goods = o.CodAmount;
        if (partial)
        {
            if (!o.AllowPartial)
                return "PARTIAL_NOT_ALLOWED";
            int refused = 0;
            foreach (OrderItem it in o.Items)
            {
                if (it.Refused)
                {
                    refused++;
                    goods -= it.Price;
                }
            }
            o.History.Add("Partial " + (o.Items.Count - refused) + "/" + o.Items.Count);
        }
        decimal total = goods + Cash.Commission(o, goods) + fee;
        // після 20:00 інкасації нема, великі суми - карткою
        if (total > 10000m && DateTime.Now.Hour >= 20)
            return "CARD_ONLY";
        Cash.Take(total, o.Number);
        o.Status = OrderStatus.HandedOut;
        o.HandedOutAt = DateTime.Now;
        o.PaidAmount = total;
        o.PaidFee = fee;
        o.History.Add("HandedOut");
        // SmsGateway.Send(o.Phone, "Thank you! Order " + o.Number);
        Storage.BusyCells.Remove(o.Cell);
        PresentedId = null;
        HandoutsToday++;
        LastAmount = total;
        DeskJournal.Write("HANDOUT " + o.Number + " " + total);
        return "OK";
    }

    // стара формула (до 2023): 7 днів безкоштовно
    private decimal OldFee(int days)
    {
        return days > 7 ? (days - 7) * 10m : 0m;
    }
}
