namespace PickupPoint;

// R09: єдина нормалізація телефону для приймання й пошуку.
// Було дві копії по 7 рядків (StorageService, OrderStore) — стала одна.
public static class PhoneNormalizer
{
    public static string Normalize(string phone)
    {
        string p = phone.Trim();
        p = p.Replace(" ", "").Replace("-", "");
        p = p.Replace("(", "").Replace(")", "");
        if (p.StartsWith("+38"))
            p = p.Substring(3);
        if (p.Length == 12 && p.StartsWith("38"))
            p = p.Substring(2);
        return p;
    }
}
