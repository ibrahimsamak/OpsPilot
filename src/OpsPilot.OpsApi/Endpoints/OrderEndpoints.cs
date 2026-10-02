using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpsPilot.OpsApi.Contracts;
using OpsPilot.OpsApi.Data;
using OpsPilot.ServiceDefaults;

namespace OpsPilot.OpsApi.Endpoints;

public static class OrderEndpoints
{
    private const int MaxPaymentAttempts = 3;

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders").RequireAuthorization(OpsPolicies.CanView);

        orders.MapGet("/{id:int}", async (int id, OpsDbContext db, CancellationToken ct) =>
        {
            var order = await db.Orders.AsNoTracking()
                .Include(o => o.Lines)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            return order is null
                ? Results.NotFound(new { error = $"Order {id} not found." })
                : Results.Ok(OrderDto.From(order));
        });

        orders.MapPost("/{id:int}/payments/retry", async (
            int id, OpsDbContext db, ClaimsPrincipal user, ILoggerFactory loggers, CancellationToken ct) =>
        {
            var order = await db.Orders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.Id == id, ct);
            if (order is null)
                return Results.NotFound(new { error = $"Order {id} not found." });

            if (order.Status != OrderStatus.PaymentFailed)
                return Results.Conflict(new { error = $"Order {id} is {order.Status}; only PaymentFailed orders can be retried." });

            var last = order.Payments.OrderByDescending(p => p.AttemptedAt).First();

            if (last.FailureCode == "fraud_suspected")
                return Results.Conflict(new { error = "Retry blocked: fraud_suspected orders must go through fraud review." });

            if (order.Payments.Count >= MaxPaymentAttempts)
                return Results.Conflict(new { error = $"Maximum {MaxPaymentAttempts} payment attempts reached; ask the customer for a new payment method." });

            var succeeded = last.FailureCode == "gateway_timeout"; // simulated gateway has recovered
            var attempt = new Payment
            {
                Amount = order.Total,
                Provider = last.Provider,
                AttemptedAt = DateTimeOffset.UtcNow,
                Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
                FailureCode = succeeded ? null : last.FailureCode,
                FailureMessage = succeeded ? null : last.FailureMessage
            };
            order.Payments.Add(attempt);
            if (succeeded) order.Status = OrderStatus.Paid;

            await db.SaveChangesAsync(ct);

            loggers.CreateLogger("OpsPilot.OpsApi.Audit").LogInformation(
                "Payment retry for order {OrderId} by {User}: {Outcome}",
                id, user.Identity?.Name, succeeded ? "succeeded" : "failed");

            return Results.Ok(new
            {
                orderId = id,
                outcome = succeeded ? "succeeded" : "failed",
                orderStatus = order.Status.ToString(),
                attemptsUsed = order.Payments.Count,
                payment = PaymentDto.From(attempt)
            });
        })
        .RequireAuthorization(OpsPolicies.CanOperate);

        return app;
    }
}
