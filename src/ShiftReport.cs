namespace PickupPoint;

// Звіти зміни: полиці й закриття каси
public class ShiftReport
{
    public OrderStore Store;
    public CashBox Cash;

    // що лежить і скільки доплатить клієнт
    public void PrintShelf()
    {
        Console.WriteLine("Shelf:");
        foreach (PickupOrder o in Store.Orders)
        {
            if (o.Status != OrderStatus.Stored)
                continue;
            int days = (DateTime.Today - o.ArrivedAt.Date).Days;
            decimal rate = 15m;
            if (o.Size == "L")
                rate = 25m;
            decimal fee = 0m;
            if (days > 5)
                fee = (days - 5) * rate;
            Console.WriteLine("  " + o.Line() + " | fee " + fee.ToString("0.00"));
        }
    }

    public void CloseShift()
    {
        int handed = 0, returned = 0;
        decimal cod = 0m;
        foreach (PickupOrder o in Store.Orders)
        {
            if (o.Status == OrderStatus.HandedOut)
                handed++;
            if (o.Status == OrderStatus.HandedOut && o.IsCod())
                cod += o.PaidAmount;
            if (o.Status == OrderStatus.Returned || o.Status == OrderStatus.Expired)
                returned++;   // протерміновані теж їдуть назад
        }
        Console.WriteLine("Handed out " + handed + ", returned " + returned
            + ", COD " + cod.ToString("0.00"));
        Console.WriteLine("Cash in " + Cash.TakenToday.ToString("0.00") + ", out "
            + Cash.RefundedToday.ToString("0.00") + ", drawer " + CashBox.Balance.ToString("0.00"));
    }
}
