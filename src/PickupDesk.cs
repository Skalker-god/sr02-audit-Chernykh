namespace PickupPoint;

// Стійка видачі: видача, повернення, частковий викуп
public class PickupDesk
{
    private const int ReturnPeriodDays = 14;
    private const decimal IdRequiredFrom = 5000m;
    private const decimal CardOnlyFrom = 10000m;
    private const int CardOnlyHour = 20;
    public static int HandoutsToday = 0;
    public OrderStore Store;
    public StorageService Storage;
    public CashBox Cash = new CashBox();
    private readonly StorageFeeCalculator _fees = new StorageFeeCalculator();
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
        return o != null && o.Refuse(sku);
    }

    // R19: сценарії мають імена, а не набір булевих прапорців.
    public string Handout(string code, string phone)
        => Process(new HandoutRequest { Code = code, Phone = phone });

    public string HandoutByProxy(string code)
        => Process(new HandoutRequest { Code = code, SkipCheck = true });

    public string HandoutPartially(string code, string phone)
        => Process(new HandoutRequest { Code = code, Phone = phone, Partial = true });

    public string ReturnOrder(string code, string phone)
        => Process(new HandoutRequest { Code = code, Phone = phone, IsReturn = true });

    // R01: диспетчер лише обирає сценарій, деталі — в окремих методах.
    private string Process(HandoutRequest req)
    {
        LastAmount = 0m;
        DateTime moment = Clock();
        PickupOrder o = Store.FindByCode(req.Code);
        if (o == null)
            return "NOT_FOUND";
        if (!req.SkipCheck)
        {
            string wrongPhone = CheckPhone(o, req.Phone, req.IsReturn);
            if (wrongPhone != null)
                return wrongPhone;
        }
        return req.IsReturn
            ? ProcessReturn(o, moment)
            : ProcessHandout(o, moment, req);
    }

    private string CheckPhone(PickupOrder o, string phone, bool isReturn)
    {
        List<PickupOrder> mine = Store.FindByPhone(phone, isReturn);
        if (mine.Contains(o))
            return null;
        o.FailedAttempts++;   // TODO TD-03: блокувати після 3 спроб; строк — до 2027-01-31.
        return "WRONG_PHONE";
    }

    private string ProcessReturn(PickupOrder o, DateTime moment)
    {
        if (o.Status == OrderStatus.Stored)
            return RefuseBeforeHandout(o);
        if (o.Status != OrderStatus.HandedOut)
            return "ERR_STATUS " + o.Status;
        return RefundReturned(o, moment);
    }

    private string RefuseBeforeHandout(PickupOrder o)
    {
        // відмова до видачі: річ ще на полиці
        o.Status = OrderStatus.Returned;
        o.History.Add("Refused");
        return "REFUSED";
    }

    private string RefundReturned(PickupOrder o, DateTime moment)
    {
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

    private string ProcessHandout(PickupOrder o, DateTime moment,
        HandoutRequest req)
    {
        if (o.Status != OrderStatus.Stored)
            return RejectNotStored(o);
        if (!req.SkipCheck)
        {
            string expired = CheckStorage(o, moment);
            if (expired != null)
                return expired;
        }
        if (NeedsId(o))
            return "ID_REQUIRED";
        decimal fee = _fees.Calculate(o, moment);
        decimal goods = o.CodAmount;
        if (req.Partial)
        {
            string denied = ApplyPartial(o, ref goods);
            if (denied != null)
                return denied;
        }
        return CompleteHandout(o, moment, goods, fee);
    }

    private string RejectNotStored(PickupOrder o)
    {
        // повторна видача або видача з поверненого стану
        Log("  ! " + o.Number + " is " + o.Status + ", cannot hand out");
        return "ERR_STATUS " + o.Status;
    }

    private string CheckStorage(PickupOrder o, DateTime moment)
    {
        if (moment.Date <= o.StorageUntil)
            return null;
        o.Status = OrderStatus.Expired;
        o.History.Add("Expired");
        Log("  ! storage term of " + o.Number + " is over");
        return "EXPIRED";
    }

    private bool NeedsId(PickupOrder o)
        => o.DeclaredValue >= IdRequiredFrom && PresentedId == null;

    private string ApplyPartial(PickupOrder o, ref decimal goods)
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
        return null;
    }

    private string CompleteHandout(PickupOrder o, DateTime moment,
        decimal goods, decimal fee)
    {
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
        Storage.BusyCells.Remove(o.Cell);
        PresentedId = null;
        HandoutsToday++;
        LastAmount = total;
        DeskJournal.Write("HANDOUT " + o.Number + " " + total);
        return "OK";
    }

}
