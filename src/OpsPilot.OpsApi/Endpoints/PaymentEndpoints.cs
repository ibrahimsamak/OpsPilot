using Microsoft.EntityFrameworkCore;
using OpsPilot.OpsApi.Contracts;
using OpsPilot.OpsApi.Data;
using OpsPilot.ServiceDefaults;

namespace OpsPilot.OpsApi.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/payments/failures", async (int? sinceHours, OpsDbContext db, CancellationToken ct) =>
        {
            var hours = Math.Clamp(sinceHours ?? 24, 1, 168);
            var since = DateTimeOffset.UtcNow.AddHours(-hours);

            var failures = await db.Payments.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Failed && p.AttemptedAt >= since)
                .OrderByDescending(p => p.AttemptedAt)
                .Select(p => new PaymentFailureDto(p.OrderId, p.Amount, p.FailureCode!, p.FailureMessage, p.AttemptedAt))
                .ToListAsync(ct);

            var totalAttempts = await db.Payments.CountAsync(p => p.AttemptedAt >= since, ct);

            var byCode = failures
                .GroupBy(f => f.FailureCode)
                .Select(g => new
                {
                    code = g.Key,
                    failures = g.Count(),
                    distinctOrders = g.Select(x => x.OrderId).Distinct().Count(),
                    amount = g.Sum(x => x.Amount),
                    lastSeen = g.Max(x => x.AttemptedAt)
                })
                .OrderByDescending(x => x.failures);

            return Results.Ok(new
            {
                windowHours = hours,
                since,
                totalPaymentAttempts = totalAttempts,
                totalFailures = failures.Count,
                failureRatePercent = totalAttempts == 0 ? 0 : Math.Round(100.0 * failures.Count / totalAttempts, 1),
                failedAmount = failures.Sum(f => f.Amount),
                byCode,
                recent = failures.Take(25)
            });
        })
        .RequireAuthorization(OpsPolicies.CanView);

        return app;
    }
}
