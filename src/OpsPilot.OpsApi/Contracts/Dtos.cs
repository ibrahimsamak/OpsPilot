using OpsPilot.OpsApi.Data;

namespace OpsPilot.OpsApi.Contracts;

public sealed record OrderLineDto(string Sku, int Quantity, decimal UnitPrice);

public sealed record PaymentDto(
    int Id, decimal Amount, string Status, string Provider,
    string? FailureCode, string? FailureMessage, DateTimeOffset AttemptedAt)
{
    public static PaymentDto From(Payment p) =>
        new(p.Id, p.Amount, p.Status.ToString(), p.Provider, p.FailureCode, p.FailureMessage, p.AttemptedAt);
}

public sealed record OrderDto(
    int Id, string CustomerRef, string Status, decimal Total, DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDto> Lines, IReadOnlyList<PaymentDto> Payments)
{
    public static OrderDto From(Order o) => new(
        o.Id, o.CustomerRef, o.Status.ToString(), o.Total, o.CreatedAt,
        o.Lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice)).ToList(),
        o.Payments.OrderBy(p => p.AttemptedAt).Select(PaymentDto.From).ToList());
}

public sealed record PaymentFailureDto(
    int OrderId, decimal Amount, string FailureCode, string? FailureMessage, DateTimeOffset AttemptedAt);

public sealed record ReserveStockRequest(int Quantity, int? OrderId, string Reason);
