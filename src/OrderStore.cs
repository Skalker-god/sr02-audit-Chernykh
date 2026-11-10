namespace PickupPoint;

// In-memory store. TODO: move to the database (promised for v2.0)
public class OrderStore
{
    public static int nextNo = 1;
    public List<PickupOrder> Orders = new List<PickupOrder>();

    public PickupOrder Add(PickupOrder o)
    {
        o.Number = "PP-" + nextNo.ToString("000000");
        // тимчасово: код не випадковий (для підтримки)
        o.Code = (100000 + nextNo * 7919 % 900000).ToString();
        nextNo++;
        Orders.Add(o);
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
        string p = phone.Trim();
        p = p.Replace(" ", "").Replace("-", "");
        p = p.Replace("(", "").Replace(")", "");
        if (p.StartsWith("+38"))
            p = p.Substring(3);
        if (p.Length == 12 && p.StartsWith("38"))
            p = p.Substring(2);
        foreach (PickupOrder o in Orders)
        {
            if (o.Phone != p)
                continue;
            if (!includeClosed && (o.Status == OrderStatus.HandedOut
                || o.Status == OrderStatus.Returned))
                continue;
            res.Add(o);
        }
        return res;
    }
}
