using PickupPoint;
using Xunit;

namespace Legacy.Tests;

/// <summary>
/// Характеризаційні тести успадкованого коду. Очікувані значення взяті з
/// поведінки оригіналу, а не з бажаної. Після R19 змінюються лише виклики.
/// </summary>
public class PickupDeskCharacterizationTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 10, 0, 0);

    private static PickupDesk NewDesk(DateTime now)
    {
        OrderStore.nextNo = 1;
        CashBox.Balance = 0m;
        PickupDesk.HandoutsToday = 0;
        DeskJournal.Enabled = false;
        StorageService.NotifyBySms = false;

        var store = new OrderStore();
        return new PickupDesk(store)
        {
            Clock = () => now,
            Log = _ => { },
        };
    }

    private static PickupOrder AddOrder(
        PickupDesk desk,
        int daysAgo,
        string size = "M",
        decimal declared = 0m,
        decimal cod = 0m,
        bool allowPartial = false,
        params OrderItem[] items)
    {
        PickupOrder order = desk.Storage.RegisterArrival(
            "Test Client", "0671234567", size, declared, cod,
            allowPartial, items.ToList());
        order.ArrivedAt = Now.Date.AddDays(-daysAgo);
        desk.Storage.PlaceOnShelf(order);
        return order;
    }

    private static OrderItem[] OneItem(decimal price = 0m) =>
        new[] { new OrderItem("SKU-1", price) };

    // ---------- розрахунки ----------

    [Fact]
    public void Handout_NormalStoredOrder_ReturnsOk()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 2, cod: 1000m, items: OneItem(1000m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(OrderStatus.HandedOut, order.Status);
        Assert.Equal(1040m, desk.LastAmount);
        Assert.Equal(Now, order.HandedOutAt);
        Assert.Equal(1, PickupDesk.HandoutsToday);
        Assert.Contains("HandedOut", order.History);
        Assert.Equal(1040m, CashBox.Balance);
    }

    [Fact]
    public void Handout_StorageExactlyFiveDays_NoFee()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 5, cod: 100m, items: OneItem(100m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(122m, desk.LastAmount);
    }

    [Fact]
    public void Handout_StorageSixDays_AddsFee()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 6, cod: 0m, items: OneItem());

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(15m, desk.LastAmount);
    }

    [Fact]
    public void Handout_LargeSizeSevenDays_Uses25Rate()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 7, size: "L", cod: 0m, items: OneItem());

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(50m, desk.LastAmount);
    }

    [Fact]
    public void Handout_PartialRefusedItems_ReducesGoods()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(
            desk, 0, cod: 600m, allowPartial: true,
            items: new[]
            {
                new OrderItem("SKU-1", 100m),
                new OrderItem("SKU-2", 200m),
                new OrderItem("SKU-3", 300m),
            });
        desk.RefuseItem(order.Code, "SKU-2");

        string result = desk.HandoutPartially(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(428m, desk.LastAmount);
        Assert.Contains("Partial 2/3", order.History);
    }

    [Fact]
    public void Handout_PartialNotAllowed_ReturnsError()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, allowPartial: false, items: OneItem());

        string result = desk.HandoutPartially(order.Code, order.Phone);

        Assert.Equal("PARTIAL_NOT_ALLOWED", result);
        Assert.Equal(OrderStatus.Stored, order.Status);
        Assert.Equal(0m, desk.LastAmount);
    }

    [Fact]
    public void Handout_PartialEmptyItems_ReturnsOk()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: 100m, allowPartial: true, items: OneItem(100m));
        order.Items.Clear();

        string result = desk.HandoutPartially(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(122m, desk.LastAmount);
        Assert.Contains("Partial 0/0", order.History);
    }

    [Fact]
    public void Handout_DeclaredValue4999WithoutId_ReturnsOk()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, declared: 4999m, items: OneItem());

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
    }

    [Fact]
    public void Handout_DeclaredValue5000WithoutId_ReturnsIdRequired()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, declared: 5000m, items: OneItem());

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("ID_REQUIRED", result);
        Assert.Equal(OrderStatus.Stored, order.Status);
    }

    // ---------- помилки ----------

    [Fact]
    public void Handout_ExpiredStorage_ReturnsExpired()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 8, items: OneItem());

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("EXPIRED", result);
        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.Contains("Expired", order.History);
    }

    [Fact]
    public void Handout_WrongPhone_ReturnsWrongPhone()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, items: OneItem());

        string result = desk.Handout(order.Code, "0670000000");

        Assert.Equal("WRONG_PHONE", result);
        Assert.Equal(1, order.FailedAttempts);
        Assert.Equal(OrderStatus.Stored, order.Status);
    }

    [Fact]
    public void Handout_UnknownCode_ReturnsNotFound()
    {
        PickupDesk desk = NewDesk(Now);

        string result = desk.Handout("999999", "0671234567");

        Assert.Equal("NOT_FOUND", result);
    }

    [Fact]
    public void Handout_NullCode_ReturnsNotFound()
    {
        PickupDesk desk = NewDesk(Now);

        string result = desk.Handout(null, "0671234567");

        Assert.Equal("NOT_FOUND", result);
    }

    // ---------- повернення і стани ----------

    [Fact]
    public void Handout_ReturnStoredOrder_ReturnsRefused()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, items: OneItem());

        string result = desk.ReturnOrder(order.Code, order.Phone);

        Assert.Equal("REFUSED", result);
        Assert.Equal(OrderStatus.Returned, order.Status);
        Assert.Contains("Refused", order.History);
    }

    [Fact]
    public void Handout_ReturnOnFourteenthDay_Refunds()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: 1000m, items: OneItem(1000m));
        desk.Handout(order.Code, order.Phone);
        order.HandedOutAt = Now.AddDays(-14);

        string result = desk.ReturnOrder(order.Code, order.Phone);

        Assert.Equal("RETURNED", result);
        Assert.Equal(OrderStatus.Returned, order.Status);
        Assert.Equal(1040m, desk.LastAmount);
        Assert.Equal(0m, CashBox.Balance);
    }

    [Fact]
    public void Handout_ReturnOnFifteenthDay_ReturnsPeriodOver()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: 1000m, items: OneItem(1000m));
        desk.Handout(order.Code, order.Phone);
        order.HandedOutAt = Now.AddDays(-15);

        string result = desk.ReturnOrder(order.Code, order.Phone);

        Assert.Equal("RETURN_PERIOD_OVER", result);
        Assert.Equal(OrderStatus.HandedOut, order.Status);
    }

    [Fact]
    public void Handout_ReturnWrongStatus_ReturnsErrStatus()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, items: OneItem());
        order.Status = OrderStatus.Arrived;

        string result = desk.ReturnOrder(order.Code, order.Phone);

        Assert.Equal("ERR_STATUS Arrived", result);
    }

    [Fact]
    public void Handout_HandoutWrongStatus_ReturnsErrStatus()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, items: OneItem());
        order.Status = OrderStatus.Returned;

        string result = desk.HandoutByProxy(order.Code);

        Assert.Equal("ERR_STATUS Returned", result);
    }

    [Fact]
    public void Handout_ByProxy_SkipsPhoneCheck()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: 100m, items: OneItem(100m));

        string result = desk.HandoutByProxy(order.Code);

        Assert.Equal("OK", result);
        Assert.Equal(OrderStatus.HandedOut, order.Status);
    }

    // ---------- картка і час ----------

    [Fact]
    public void Handout_TotalOver10000At20_ReturnsCardOnly()
    {
        PickupDesk desk = NewDesk(new DateTime(2026, 11, 9, 20, 30, 0));
        PickupOrder order = AddOrder(desk, 0, cod: 10000m, items: OneItem(10000m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("CARD_ONLY", result);
        Assert.Equal(OrderStatus.Stored, order.Status);
        Assert.Equal(0m, desk.LastAmount);
    }

    [Fact]
    public void Handout_TotalOver10000At19_ReturnsOk()
    {
        PickupDesk desk = NewDesk(new DateTime(2026, 11, 9, 19, 30, 0));
        PickupOrder order = AddOrder(desk, 0, cod: 10000m, items: OneItem(10000m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(10220m, desk.LastAmount);
    }

    [Fact]
    public void Handout_TotalExactly10000At20_ReturnsOk()
    {
        PickupDesk desk = NewDesk(new DateTime(2026, 11, 9, 20, 0, 0));
        PickupOrder order = AddOrder(desk, 0, cod: 9784.31m, items: OneItem(9784.31m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(10000.00m, desk.LastAmount);
    }

    [Fact]
    public void Handout_Total10000Point01At20_ReturnsCardOnly()
    {
        PickupDesk desk = NewDesk(new DateTime(2026, 11, 9, 20, 0, 0));
        PickupOrder order = AddOrder(desk, 0, cod: 9784.32m, items: OneItem(9784.32m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("CARD_ONLY", result);
    }

    [Theory]
    [InlineData("+38 (067) 123-45-67")]
    [InlineData("380671234567")]
    [InlineData("067-123-45-67")]
    public void Handout_PhoneFormat_ReturnsOk(string phone)
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: 100m, items: OneItem(100m));

        string result = desk.Handout(order.Code, phone);

        Assert.Equal("OK", result);
        Assert.Equal(OrderStatus.HandedOut, order.Status);
    }

    [Fact]
    public void Handout_NegativeCodAmount_Characterization()
    {
        // Характеризаційний тест: наявна поведінка, а не бажана.
        // Від'ємний COD не блокується: каса зменшується, статус — HandedOut.
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: -100m, items: OneItem(100m));

        string result = desk.Handout(order.Code, order.Phone);

        Assert.Equal("OK", result);
        Assert.Equal(-100m, desk.LastAmount);
        Assert.Equal(-100m, CashBox.Balance);
        Assert.Equal(OrderStatus.HandedOut, order.Status);
    }

    // ---------- відмова, приймання, стани ----------

    [Fact]
    public void RefuseItem_ExistingSku_MarksRefused()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, items: OneItem());

        bool result = desk.RefuseItem(order.Code, "SKU-1");

        Assert.True(result);
        Assert.True(order.Items[0].Refused);
        Assert.False(desk.RefuseItem(order.Code, "SKU-404"));
        Assert.False(desk.RefuseItem("999999", "SKU-1"));
    }

    [Fact]
    public void RegisterArrival_InvalidPhone_ReturnsNull()
    {
        PickupDesk desk = NewDesk(Now);

        PickupOrder order = desk.Storage.RegisterArrival(
            "Test Client", "12345", "M", 0m, 0m, false, new List<OrderItem> { new("SKU-1", 0m) });

        Assert.Null(order);
        Assert.Empty(desk.Store.Orders);
    }

    [Fact]
    public void RegisterArrival_EmptyName_ReturnsNull()
    {
        PickupDesk desk = NewDesk(Now);

        PickupOrder order = desk.Storage.RegisterArrival(
            "", "0671234567", "M", 0m, 0m, false, new List<OrderItem> { new("SKU-1", 0m) });

        Assert.Null(order);
    }

    [Fact]
    public void RegisterArrival_NullSize_KeepsDefaultM()
    {
        // Характеризаційний тест: try/catch навколо size.Trim() ковтає
        // NullReferenceException, тому розмір лишається M.
        PickupDesk desk = NewDesk(Now);

        PickupOrder order = desk.Storage.RegisterArrival(
            "Test Client", "+38 (067) 123-45-67", null, 0m, 0m, false,
            new List<OrderItem> { new("SKU-1", 0m) });

        Assert.NotNull(order);
        Assert.Equal("0671234567", order.Phone);
        Assert.Equal("M", order.Size);
        Assert.Equal("PP-000001", order.Number);
        Assert.Contains("Arrived", order.History);
    }

    [Fact]
    public void ChangeStatus_AllowedAndForbiddenTransitions_ReturnExpected()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, items: OneItem());

        bool toHandedOut = desk.Storage.ChangeStatus(order.Number, OrderStatus.HandedOut);
        bool backToStored = desk.Storage.ChangeStatus(order.Number, OrderStatus.Stored);
        bool unknown = desk.Storage.ChangeStatus("PP-999999", OrderStatus.Stored);

        Assert.True(toHandedOut);
        Assert.Equal(OrderStatus.HandedOut, order.Status);
        Assert.False(backToStored);
        Assert.False(unknown);
    }

    [Fact]
    public void AssignCell_HighDeclaredValue_UsesSafePrefix()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, declared: 5000m, items: OneItem());

        Assert.StartsWith("SAFE-", order.Cell);
    }

    [Fact]
    public void AssignCell_LargeSize_UsesFloorPrefix()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, size: "L", items: OneItem());

        Assert.StartsWith("F-", order.Cell);
    }

    [Fact]
    public void AssignCell_AllCellsBusy_ReturnsNull()
    {
        PickupDesk desk = NewDesk(Now);
        for (int i = 1; i <= 40; i++)
            desk.Storage.BusyCells.Add("B-" + i.ToString("00"));
        PickupOrder order = new PickupOrder { Size = "M" };

        string cell = desk.Storage.AssignCell(order);

        Assert.Null(cell);
    }

    [Fact]
    public void FindByPhone_ClosedOrder_ExcludedByDefault()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 0, cod: 100m, items: OneItem(100m));
        desk.Handout(order.Code, order.Phone);

        Assert.Empty(desk.Store.FindByPhone(order.Phone, false));
        Assert.Single(desk.Store.FindByPhone(order.Phone, true));
    }

    [Fact]
    public void SweepExpired_StoredPastDue_MarksExpiredAndBills()
    {
        PickupDesk desk = NewDesk(Now);
        PickupOrder order = AddOrder(desk, 9, items: OneItem());
        // SweepExpired() використовує DateTime.Today, тому дати прив'язуємо до
        // реального дня запуску, а не до фіксованого Now.
        order.ArrivedAt = DateTime.Today.AddDays(-9);
        order.StorageUntil = DateTime.Today.AddDays(-2);

        int count = desk.Storage.SweepExpired();

        Assert.Equal(1, count);
        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.Equal(60m, desk.Storage.SenderFees);
        Assert.Empty(desk.Storage.BusyCells);
    }

    [Fact]
    public void ShiftReports_StoredAndHandedOut_PrintWithoutExceptions()
    {
        PickupDesk desk = NewDesk(Now);
        AddOrder(desk, 7, cod: 100m, items: OneItem(100m));
        var report = new ShiftReport { Store = desk.Store, Cash = desk.Cash };

        report.PrintShelf();
        report.CloseShift();
    }
}
