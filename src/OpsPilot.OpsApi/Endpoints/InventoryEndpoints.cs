using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpsPilot.OpsApi.Contracts;
using OpsPilot.OpsApi.Data;
using OpsPilot.ServiceDefaults;

namespace OpsPilot.OpsApi.Endpoints;

public static class InventoryEndpoints
{
    private const int MaxUnitsPerReservation = 50;
    private const int LowStockThreshold = 5;

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var inventory = app.MapGroup("/inventory").RequireAuthorization(OpsPolicies.CanView);

        inventory.MapGet("/{sku}", async (string sku, OpsDbContext db, CancellationToken ct) =>
        {
            sku = sku.Trim().ToUpperInvariant();
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);
            if (product is null)
                return Results.NotFound(new { error = $"SKU {sku} not found." });

            var recent = await db.Reservations.AsNoTracking()
                .Where(r => r.Sku == sku)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new { r.Id, r.OrderId, r.Quantity, r.Reason, r.CreatedBy, r.CreatedAt })
                .ToListAsync(ct);

            var available = product.OnHand - product.Reserved;
            return Results.Ok(new
            {
                product.Sku,
                product.Name,
                product.Price,
                product.OnHand,
                product.Reserved,
                available,
                lowStock = available <= LowStockThreshold,
                recentReservations = recent
            });
        });

        inventory.MapPost("/{sku}/reservations", async (
            string sku, ReserveStockRequest request, OpsDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            sku = sku.Trim().ToUpperInvariant();
            if (request.Quantity is < 1 or > MaxUnitsPerReservation)
                return Results.BadRequest(new { error = $"quantity must be between 1 and {MaxUnitsPerReservation}." });
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { error = "reason is required." });

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);
            if (product is null)
                return Results.NotFound(new { error = $"SKU {sku} not found." });

            var updated = await db.Products
                .Where(p => p.Sku == sku && p.OnHand - p.Reserved >= request.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Reserved, p => p.Reserved + request.Quantity), ct);

            if (updated == 0)
            {
                var available = product.OnHand - product.Reserved;
                return Results.Conflict(new { error = $"Only {available} units of {sku} are available.", available });
            }

            var reservation = new StockReservation
            {
                Sku = sku,
                OrderId = request.OrderId,
                Quantity = request.Quantity,
                Reason = request.Reason.Trim(),
                CreatedBy = user.Identity?.Name ?? "unknown",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var availableAfter = await db.Products.AsNoTracking()
                .Where(p => p.Sku == sku)
                .Select(p => p.OnHand - p.Reserved)
                .FirstAsync(ct);

            return Results.Ok(new
            {
                reservationId = reservation.Id,
                sku,
                reserved = request.Quantity,
                orderId = request.OrderId,
                availableAfter,
                lowStock = availableAfter <= LowStockThreshold,
                createdBy = reservation.CreatedBy
            });
        })
        .RequireAuthorization(OpsPolicies.CanOperate);

        return app;
    }
}
