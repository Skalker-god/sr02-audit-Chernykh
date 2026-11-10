namespace PickupPoint;

// Демонстраційний сценарій зміни
// TODO TD-05: демонстраційний Main не покритий тестами; строк — до 2027-03-31.
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
        int n = desk.Storage.SweepExpired();
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
        o.ArrivedAt = DateTime.Today.AddDays(-daysAgo);   // дата надходження
        s.PlaceOnShelf(o);
        Console.WriteLine("  " + o.Line() + " | code " + o.Code);
        return o;
    }

    // R19: помічник більше не приймає булевих прапорців.
    static void Show(PickupDesk desk, PickupOrder o, string what, string result)
    {
        Console.WriteLine(o.Number + " " + what + ": " + result + " "
            + desk.LastAmount.ToString("0.00"));
    }

    static void Day(PickupDesk desk, PickupOrder[] o)
    {
        Show(desk, o[1], "hand out", desk.Handout(o[1].Code, "067 123 45 67"));
        Show(desk, o[4], "wrong phone", desk.Handout(o[4].Code, "0670000000"));
        Console.WriteLine("Code 999999: " + desk.Handout("999999", ""));
        Show(desk, o[2], "hand out", desk.Handout(o[2].Code, "0501112233"));
        Show(desk, o[2], "again", desk.Handout(o[2].Code, "0501112233"));
        Show(desk, o[3], "no ID", desk.Handout(o[3].Code, "0675550102"));
        desk.PresentedId = "FA123456";
        Show(desk, o[3], "with ID", desk.Handout(o[3].Code, "0675550102"));
        Show(desk, o[8], "partial", desk.HandoutPartially(o[8].Code, "0687778899"));
        Show(desk, o[8], "refusal", desk.ReturnOrder(o[8].Code, "0687778899"));
        Show(desk, o[8], "by proxy", desk.HandoutByProxy(o[8].Code));
        Console.WriteLine("Refuse SKU-402: " + desk.RefuseItem(o[4].Code, "SKU-402"));
        Console.WriteLine("Refuse SKU-999: " + desk.RefuseItem(o[4].Code, "SKU-999"));
        Show(desk, o[4], "partial", desk.HandoutPartially(o[4].Code, "+380931234567"));
        desk.RefuseItem(o[5].Code, "SKU-501");
        desk.RefuseItem(o[5].Code, "SKU-502");
        Show(desk, o[5], "all refused", desk.HandoutPartially(o[5].Code, "0661002030"));
        Show(desk, o[6], "by proxy", desk.HandoutByProxy(o[6].Code));
        Console.WriteLine("=== Returns ===");
        Show(desk, o[4], "return", desk.ReturnOrder(o[4].Code, "0931234567"));
        o[1].HandedOutAt = DateTime.Today.AddDays(-15);   // пізніше 15 днів
        Show(desk, o[1], "return", desk.ReturnOrder(o[1].Code, "0671234567"));
        Show(desk, o[2], "return", desk.ReturnOrder(o[2].Code, "0501112233"));
        StorageService s = desk.Storage;
        Console.WriteLine("To shop: " + s.ChangeStatus(o[2].Number, OrderStatus.Returned));
        Console.WriteLine("To shelf: " + s.ChangeStatus(o[8].Number, OrderStatus.Stored));
        Console.WriteLine("To shelf: " + s.ChangeStatus("PP-999999", OrderStatus.Stored));
        Console.WriteLine(o[4].Number + " history: " + string.Join(" > ", o[4].History));
    }
}
