namespace PickupPoint;

// In-memory store. TODO: move to the database (promised for v2.0)
public class OrderStore
{
    public static int nextNo = 1;
    private readonly List<PickupOrder> _orders = new List<PickupOrder>();

    // R13: колекція доступна лише для читання, змінювати її можна тільки через Add.
    public IReadOnlyList<PickupOrder> Orders => _orders;

    public PickupOrder Add(PickupOrder o)
    {
        o.Number = "PP-" + nextNo.ToString("000000");
        // тимчасово: код не випадковий (для підтримки)
        o.Code = (100000 + nextNo * 7919 % 900000).ToString();
        nextNo++;
        _orders.Add(o);
        return o;
    }

    public PickupOrder FindByCode(string code)
    {
        if (code == null)
            return null;
        foreach (PickupOrder o in Orders)
            if (o.Code == code.Trim())   // сканер додає пробіл
                return o;
        return null;
    }

    public PickupOrder FindByNumber(string number)
    {
        foreach (PickupOrder o in Orders)
            if (o.Number == number)
                return o;
        return null;
    }

    // includeClosed - разом із виданими й поверненими
    public List<PickupOrder> FindByPhone(string phone, bool includeClosed)
    {
        List<PickupOrder> res = new List<PickupOrder>();
        if (string.IsNullOrEmpty(phone))
            return res;
        string p = PhoneNormalizer.Normalize(phone);
        foreach (PickupOrder o in Orders)
        {
            if (o.Phone != p)
                continue;
            // R03: закритий стан має ім'я, а не ланцюжок порівнянь.
            if (!includeClosed && IsClosed(o))
                continue;
            res.Add(o);
        }
        return res;
    }

    private static bool IsClosed(PickupOrder o)
        => o.Status == OrderStatus.HandedOut || o.Status == OrderStatus.Returned;
}
