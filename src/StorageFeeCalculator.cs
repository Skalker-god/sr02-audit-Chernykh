namespace PickupPoint;

// R09: єдине джерело формули платного зберігання.
// Було три копії (PickupDesk, ShiftReport, StorageService) — стала одна.
public class StorageFeeCalculator
{
    public const int FreeDays = 5;
    public const decimal StandardRate = 15m;
    public const decimal LargeRate = 25m;

    public decimal Calculate(PickupOrder o, DateTime moment)
    {
        int days = (moment.Date - o.ArrivedAt.Date).Days;
        decimal rate = o.Size == "L" ? LargeRate : StandardRate;
        if (days <= FreeDays)
            return 0m;
        return (days - FreeDays) * rate;
    }
}
