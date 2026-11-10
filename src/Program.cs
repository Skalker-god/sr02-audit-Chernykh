namespace PickupPoint;

// Демо-зміна пункту для нових операторів.
public class Program
{
    public static void Main(string[] args)
    {
        OrderStore store = new OrderStore();
        PickupDesk desk = new PickupDesk(store);
        Console.WriteLine("=== Arrivals ===");
        PickupOrder[] o = Arrivals(desk.Storage);
        Console.WriteLine("=== Hand out ===");
        Day(desk, o);
        Console.WriteLine("=== Night ===");
        ShiftReport report = new ShiftReport { Store = store, Cash = desk.Cash };
        report.PrintShelf();
        int n = desk.Storage.do_sweep();
        Console.WriteLine("Expired: " + n + ", billed " + desk.Storage.SenderFees.ToString("0.00"));
        Console.WriteLine("=== Shift ===");
        report.CloseShift();
        Console.WriteLine("Handouts: " + PickupDesk.HandoutsToday);
    }

    static PickupOrder[] Arrivals(StorageService s)
    {
        PickupOrder[] o = new PickupOrder[9];
        o[1] = Arrive(s, "Iryna Bondar", "+38 (067) 123-45-67", "S", 1200m, true, 0,
            new OrderItem("SKU-101", 200m), new OrderItem("SKU-102", 1000m));
        o[2] = Arrive(s, "Taras Melnyk", "050-111-22-33", "M", 0m, false, 8,
            new OrderItem("SKU-201", 800m));
        o[3] = Arrive(s, "Oksana Lysenko", "0675550102", "L", 7500m, false, 6,
            new OrderItem("SKU-301", 7500m));
        o[4] = Arrive(s, "Dmytro Savchuk", "380931234567", "M", 2100m, true, 2,
            new OrderItem("SKU-401", 900m), new OrderItem("SKU-402", 700m),
            new OrderItem("SKU-403", 500m));
        o[5] = Arrive(s, "Anna Koval", "0661002030", "S", 650m, true, 1,
            new OrderItem("SKU-501", 400m), new OrderItem("SKU-502", 250m));
        o[6] = Arrive(s, "Serhii Petrenko", "097 222 33 44", "M", 0m, false, 9,
            new OrderItem("SKU-601", 300m));
        o[7] = Arrive(s, "Maria Shevchenko", "0634445566", "M", 450m, false, 10,
            new OrderItem("SKU-701", 300m), new OrderItem("SKU-702", 150m));
        o[8] = Arrive(s, "Petro Hnatiuk", "0687778899", "S", 990m, false, 3,
            new OrderItem("SKU-801", 990m));
        Arrive(s, "Olha Rudenko", "12345", "M", 0m, false, 0, new OrderItem("SKU-901", 120m));
        return o;
    }

    static PickupOrder Arrive(StorageService s, string name, string phone, string size,
        decimal cod, bool partial, int daysAgo, params OrderItem[] items)
    {
        decimal value = 0m;
        foreach (OrderItem it in items)
            value += it.Price;
        List<OrderItem> list = new List<OrderItem>(items);
        PickupOrder o = s.RegisterArrival(name, phone, size, value, cod, partial, list);
        if (o == null)
            return null;
        o.ArrivedAt = DateTime.Today.AddDays(-daysAgo);   // лежить кілька днів
        s.PlaceOnShelf(o);
        Console.WriteLine("  " + o.Line() + " | code " + o.Code);
        return o;
    }

    static void Try(PickupDesk desk, PickupOrder o, string what, string phone,
        bool isReturn, bool skipCheck, bool partial)
    {
        string res = desk.Handout(o.Code, phone, isReturn, skipCheck, partial);
        Console.WriteLine(o.Number + " " + what + ": " + res + " "
            + desk.LastAmount.ToString("0.00"));
    }

    static void Day(PickupDesk desk, PickupOrder[] o)
    {
        Try(desk, o[1], "hand out", "067 123 45 67", false, false, false);
        Try(desk, o[4], "wrong phone", "0670000000", false, false, false);
        Console.WriteLine("Code 999999: " + desk.Handout("999999", "", false, false, false));
        Try(desk, o[2], "hand out", "0501112233", false, false, false);
        Try(desk, o[2], "again", "0501112233", false, false, false);
        Try(desk, o[3], "no ID", "0675550102", false, false, false);
        desk.PresentedId = "FA123456";
        Try(desk, o[3], "with ID", "0675550102", false, false, false);
        Try(desk, o[8], "partial", "0687778899", false, false, true);
        Try(desk, o[8], "refusal", "0687778899", true, false, false);
        Try(desk, o[8], "by proxy", null, false, true, false);
        Console.WriteLine("Refuse SKU-402: " + desk.RefuseItem(o[4].Code, "SKU-402"));
        Console.WriteLine("Refuse SKU-999: " + desk.RefuseItem(o[4].Code, "SKU-999"));
        Try(desk, o[4], "partial", "+380931234567", false, false, true);
        desk.RefuseItem(o[5].Code, "SKU-501");
        desk.RefuseItem(o[5].Code, "SKU-502");
        Try(desk, o[5], "all refused", "0661002030", false, false, true);
        Try(desk, o[6], "by proxy", null, false, true, false);
        Console.WriteLine("=== Returns ===");
        Try(desk, o[4], "return", "0931234567", true, false, false);
        o[1].HandedOutAt = DateTime.Today.AddDays(-15);   // прийшов через 15 днів
        Try(desk, o[1], "return", "0671234567", true, false, false);
        Try(desk, o[2], "return", "0501112233", true, false, false);
        StorageService s = desk.Storage;
        Console.WriteLine("To shop: " + s.ChangeStatus(o[2].Number, OrderStatus.Returned));
        Console.WriteLine("To shelf: " + s.ChangeStatus(o[8].Number, OrderStatus.Stored));
        Console.WriteLine("To shelf: " + s.ChangeStatus("PP-999999", OrderStatus.Stored));
        Console.WriteLine(o[4].Number + " history: " + string.Join(" > ", o[4].History));
    }
}
