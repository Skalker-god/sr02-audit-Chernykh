namespace PickupPoint;

public enum OrderStatus { Arrived, Stored, HandedOut, Returned, Expired }

// Позиція замовлення (для часткового викупу)
public class OrderItem
{
    public string Sku;
    public decimal Price;
    public bool Refused;   // клієнт відмовився на пункті

    public OrderItem(string sku, decimal price)
    {
        Sku = sku;
        Price = price;
    }
}

// Замовлення. Поля відкриті - їх читає каса і звіти
public class PickupOrder
{
    public string Number;
    public string Code;            // 6 digits, sent by SMS
    public string RecipientName;
    public string Phone;           // 0XXXXXXXXX
    public string Size = "M";      // S, M, L
    public OrderStatus Status = OrderStatus.Arrived;
    public DateTime ArrivedAt;
    public DateTime StorageUntil;
    public DateTime HandedOutAt;
    public string Cell;
    public decimal DeclaredValue;
    public decimal CodAmount;      // накладений платіж, 0 - оплачено
    public decimal PaidAmount;
    public decimal PaidFee;
    public bool AllowPartial;
    public int FailedAttempts;
    public List<OrderItem> Items = new List<OrderItem>();
    public List<string> History = new List<string>();

    public bool IsCod() => CodAmount > 0;

    public string Line()
    {
        string cell = Cell == null ? "-" : Cell;
        return Number + " | " + Size + " | " + cell + " | " + Status;
    }
}
