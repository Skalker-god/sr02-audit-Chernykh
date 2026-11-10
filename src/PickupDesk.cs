namespace PickupPoint;

// Стійка видачі: видача, повернення, частковий викуп
public class PickupDesk
{
    private const int ReturnPeriodDays = 14;
    private const decimal IdRequiredFrom = 5000m;
    private const decimal CardOnlyFrom = 10000m;
    private const int CardOnlyHour = 20;
    private const int FreeStorageDays = 5;
    private const decimal StandardSizeRate = 15m;
    private const decimal LargeSizeRate = 25m;
    private const int OldFeeFreeDays = 7;
    private const decimal OldFeeRate = 10m;
    public static int HandoutsToday = 0;
    public OrderStore Store;
    public StorageService Storage;
    public CashBox Cash = new CashBox();
    public decimal LastAmount;
    public Func<DateTime> Clock = () => DateTime.Now;   // шов часу для тестів
    public Action<string> Log = Console.WriteLine;      // шов журналу для тестів
    public string PresentedId;

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

    // R19: сценарії мають імена, а не набір булевих прапорців.
    public string Handout(string code, string phone)
        => Process(code, phone, false, false, false);

    public string HandoutByProxy(string code)
        => Process(code, null, false, true, false);

    public string HandoutPartially(string code, string phone)
        => Process(code, phone, false, false, true);

    public string ReturnOrder(string code, string phone)
        => Process(code, phone, true, false, false);

    private string Process(string code, string phone,
        bool isReturn, bool skipCheck, bool partial)
    {
        LastAmount = 0m;
        DateTime moment = Clock();
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
                // відмова до видачі: річ ще на полиці
                o.Status = OrderStatus.Returned;
                o.History.Add("Refused");
                return "REFUSED";
            }
            if (o.Status != OrderStatus.HandedOut)
                return "ERR_STATUS " + o.Status;
            int d2 = (moment.Date - o.HandedOutAt.Date).Days;
            if (d2 > ReturnPeriodDays)
                return "RETURN_PERIOD_OVER";
            decimal back = o.PaidAmount - o.PaidFee;
            Cash.Refund(back, o.Number);
            o.Status = OrderStatus.Returned;
            o.History.Add("Returned");
            LastAmount = back;
            return "RETURNED";
        }
        if (o.Status != OrderStatus.Stored)
        {
            // повторна видача або видача з поверненого стану
            Log("  ! " + o.Number + " is " + o.Status + ", cannot hand out");
            return "ERR_STATUS " + o.Status;
        }
        if (!skipCheck)
        {
            if (moment.Date > o.StorageUntil)
            {
                o.Status = OrderStatus.Expired;
                o.History.Add("Expired");
                Log("  ! storage term of " + o.Number + " is over");
                return "EXPIRED";
            }
        }
        if (o.DeclaredValue >= IdRequiredFrom && PresentedId == null)
            return "ID_REQUIRED";
        int days = (moment.Date - o.ArrivedAt.Date).Days;
        decimal rate = StandardSizeRate;
        if (o.Size == "L")
            rate = LargeSizeRate;
        decimal fee = 0m;
        if (days > FreeStorageDays)
            fee = (days - FreeStorageDays) * rate;
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
        // після 20:00 картка для великих сум
        if (total > CardOnlyFrom && moment.Hour >= CardOnlyHour)
            return "CARD_ONLY";
        Cash.Take(total, o.Number);
        o.Status = OrderStatus.HandedOut;
        o.HandedOutAt = moment;
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

    // старий тариф (до 2023): 7 днів безкоштовно
    private decimal OldFee(int days)
    {
        return days > OldFeeFreeDays ? (days - OldFeeFreeDays) * OldFeeRate : 0m;
    }
}
