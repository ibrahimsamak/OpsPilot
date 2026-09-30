using Microsoft.EntityFrameworkCore;

namespace OpsPilot.OpsApi.Data;

public static class SeedData
{
    private static readonly (string Code, string Message)[] Failures =
    [
        ("card_declined", "Issuer declined the transaction (do_not_honor)."),
        ("insufficient_funds", "Card has insufficient funds."),
        ("expired_card", "Card expiry date is in the past."),
        ("gateway_timeout", "No response from payment gateway within 30 s."),
        ("fraud_suspected", "Blocked by risk engine (score above threshold)."),
    ];

    public static async Task EnsureSeededAsync(OpsDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return;

        List<Product> products =
        [
            new() { Sku = "KB-101",   Name = "Mechanical keyboard",       Price = 129.00m, OnHand = 140, Reserved = 4 },
            new() { Sku = "MS-220",   Name = "Wireless mouse",            Price = 39.00m,  OnHand = 320, Reserved = 0 },
            new() { Sku = "MN-270",   Name = "27-inch 4K monitor",        Price = 449.00m, OnHand = 35,  Reserved = 2 },
            new() { Sku = "HD-550",   Name = "Noise-cancelling headset",  Price = 199.00m, OnHand = 60,  Reserved = 0 },
            new() { Sku = "CB-USBC",  Name = "USB-C cable 2 m",           Price = 15.00m,  OnHand = 900, Reserved = 0 },
            new() { Sku = "DK-900",   Name = "Thunderbolt docking station", Price = 289.00m, OnHand = 4, Reserved = 1 },
            new() { Sku = "WC-1080",  Name = "1080p webcam",              Price = 79.00m,  OnHand = 75,  Reserved = 0 },
            new() { Sku = "LS-STAND", Name = "Aluminium laptop stand",    Price = 49.00m,  OnHand = 12,  Reserved = 0 },
        ];
        db.Products.AddRange(products);

        var rng = new Random(20260929);
        var now = DateTimeOffset.UtcNow;

        for (var id = 100; id < 160; id++)
        {
            var order = NewOrder(id, now.AddMinutes(-rng.Next(15, 47 * 60)), products, rng);

            switch (id)
            {
                case 123: Fail(order, "gateway_timeout", attempts: 2, firstAttempt: now.AddHours(-3)); break;
                case 131: Fail(order, "card_declined", attempts: 1, firstAttempt: now.AddHours(-5)); break;
                case 142: Fail(order, "fraud_suspected", attempts: 1, firstAttempt: now.AddHours(-1)); break;
                case 150: order.Status = OrderStatus.Pending; break;
                case >= 152 and <= 156:
                    Fail(order, "gateway_timeout", attempts: 1, firstAttempt: now.AddMinutes(-rng.Next(5, 55)));
                    break;
                default:
                    var roll = rng.NextDouble();
                    if (roll < 0.15)
                        Fail(order, Failures[rng.Next(0, 3)].Code, attempts: 1, firstAttempt: order.CreatedAt.AddMinutes(1));
                    else
                        Pay(order, shipped: roll > 0.6);
                    break;
            }

            db.Orders.Add(order);
        }

        await db.SaveChangesAsync(ct);
    }

    private static Order NewOrder(int id, DateTimeOffset createdAt, List<Product> products, Random rng)
    {
        var lines = products
            .OrderBy(_ => rng.Next())
            .Take(rng.Next(1, 4))
            .Select(p => new OrderLine { Sku = p.Sku, Quantity = rng.Next(1, 3), UnitPrice = p.Price })
            .ToList();

        return new Order
        {
            Id = id,
            CustomerRef = $"CUST-{1000 + (id * 37 % 900)}",
            CreatedAt = createdAt,
            Status = OrderStatus.Pending,
            Lines = lines,
            Total = lines.Sum(l => l.UnitPrice * l.Quantity)
        };
    }

    private static void Fail(Order order, string code, int attempts, DateTimeOffset firstAttempt)
    {
        var message = Failures.First(f => f.Code == code).Message;
        if (order.CreatedAt > firstAttempt) order.CreatedAt = firstAttempt.AddMinutes(-2);

        for (var i = 0; i < attempts; i++)
        {
            order.Payments.Add(new Payment
            {
                Amount = order.Total,
                Status = PaymentStatus.Failed,
                FailureCode = code,
                FailureMessage = message,
                AttemptedAt = firstAttempt.AddMinutes(i * 4)
            });
        }

        order.Status = OrderStatus.PaymentFailed;
    }

    private static void Pay(Order order, bool shipped)
    {
        order.Payments.Add(new Payment
        {
            Amount = order.Total,
            Status = PaymentStatus.Succeeded,
            AttemptedAt = order.CreatedAt.AddMinutes(1)
        });
        order.Status = shipped ? OrderStatus.Shipped : OrderStatus.Paid;
    }
}
