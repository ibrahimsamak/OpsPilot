namespace OpsPilot.OpsApi.Data;

public enum OrderStatus { Pending, Paid, PaymentFailed, Shipped, Cancelled }

public enum PaymentStatus { Succeeded, Failed }

public sealed class Product
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
}

public sealed class Order
{
    public int Id { get; set; }
    public string CustomerRef { get; set; } = "";
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string Provider { get; set; } = "stripe-sim";
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}

public sealed class StockReservation
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public int? OrderId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
