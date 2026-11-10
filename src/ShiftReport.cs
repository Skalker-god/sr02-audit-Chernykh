namespace PickupPoint;

// Звіти зміни: полиці й закриття каси
public class ShiftReport
{
    public OrderStore Store;
    public CashBox Cash;
    private readonly StorageFeeCalculator _fees = new StorageFeeCalculator();

    // що лежить і скільки доплатить клієнт
    public void PrintShelf()
    {
        Console.WriteLine("Shelf:");
        foreach (PickupOrder o in Store.Orders)
        {
            if (o.Status != OrderStatus.Stored)
                continue;
            decimal fee = _fees.Calculate(o, DateTime.Today);
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
