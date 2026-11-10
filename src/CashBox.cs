namespace PickupPoint;

// Каса пункту видачі
public class CashBox
{
    private const decimal CommissionRate = 0.02m;
    private const decimal CommissionFixed = 20m;
    public static decimal Balance = 0m;   // одна шухляда на весь пункт
    public decimal TakenToday;
    public decimal RefundedToday;

    public void Take(decimal amount, string number)
    {
        Balance += amount;
        TakenToday += amount;
        DeskJournal.Write("CASH IN " + number + " " + amount);
    }

    public bool Refund(decimal amount, string number)
    {
        if (amount > Balance)
        {
            Console.WriteLine("  ! not enough cash to refund " + number);
            return false;
        }
        Balance -= amount;
        RefundedToday += amount;
        DeskJournal.Write("CASH OUT " + number + " " + amount);
        return true;
    }

    // bank fee for sending COD money to the seller: 2% + 20 UAH
    public decimal Commission(PickupOrder o, decimal amount)
    {
        if (!o.IsCod())
            return 0m;
        return Math.Round(amount * CommissionRate + CommissionFixed, 2);
    }
}
