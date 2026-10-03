using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using OpsPilot.Orchestrator.Knowledge;
using OpsPilot.Orchestrator.Ops;
using OpsPilot.Orchestrator.Telemetry;

namespace OpsPilot.Orchestrator.Tools;

public sealed class OpsTools(
    OpsApiClient ops,
    KnowledgeSearch knowledge,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OpsTools> logger)
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    // ---------------- read tools ----------------

    [Description("Get one order by id: status, lines, total and every payment attempt with its failure code. " +
                 "Use for any question about a specific order.")]
    public Task<JsonElement> GetOrder(
        [Description("Numeric order id, for example 123.")] int orderId,
        CancellationToken cancellationToken = default)
        => Run("get_order", requiresOperator: false, () => ops.GetOrderAsync(orderId, cancellationToken));

    [Description("Summarize failed payment attempts in a recent time window: totals, failure rate, counts by failure code " +
                 "and the most recent failures. Use for questions about payment failures or incidents.")]
    public Task<JsonElement> ListPaymentFailures(
        [Description("Hours to look back, 1-168. Use 24 for 'today', 1 or 2 for 'right now'.")] int sinceHours = 24,
        CancellationToken cancellationToken = default)
        => Run("list_payment_failures", requiresOperator: false, () => ops.ListPaymentFailuresAsync(sinceHours, cancellationToken));

    [Description("Get stock for one SKU: on hand, reserved, available, low-stock flag and recent reservations.")]
    public Task<JsonElement> GetStock(
        [Description("Product SKU, for example DK-900.")] string sku,
        CancellationToken cancellationToken = default)
        => Run("get_stock", requiresOperator: false, () => ops.GetStockAsync(sku, cancellationToken));

    [Description("Search ShopCo runbooks and policies: payment failure codes, incident procedures, refunds, " +
                 "fraud review, inventory rules. Returns passages with ids that you must cite.")]
    public Task<JsonElement> SearchRunbooks(
        [Description("A short, specific query, for example 'gateway_timeout retry' or 'refund approval limit'.")] string query,
        CancellationToken cancellationToken = default)
        => Run("search_runbooks", requiresOperator: false, async () =>
        {
            var hits = await knowledge.SearchAsync(query, cancellationToken);
            return hits.Count == 0
                ? Json(new { found = false, message = "No relevant runbook passages. Tell the user the runbooks don't cover this." })
                : Json(new
                {
                    found = true,
                    passages = hits.Select(h => new { id = h.Id, heading = h.Heading, text = h.Content, score = h.Score })
                });
        });

    // ---------------- write tools ----------------

    [Description("WRITE ACTION. Reserve units of a SKU so they cannot be sold to someone else. " +
                 "Use only when the user explicitly asks to reserve or hold stock.")]
    public Task<JsonElement> ReserveStock(
        [Description("Product SKU, for example DK-900.")] string sku,
        [Description("Units to reserve, 1 to 50.")] int quantity,
        [Description("Short business reason, e.g. 'hold for order 123 while payment is retried'.")] string reason,
        [Description("Order id the reservation is for, if any.")] int? orderId = null,
        CancellationToken cancellationToken = default)
        => Run("reserve_stock", requiresOperator: true, () =>
            quantity is < 1 or > 50
                ? Task.FromResult(Json(new { error = true, detail = "quantity must be between 1 and 50" }))
                : ops.ReserveStockAsync(sku, quantity, orderId, reason, cancellationToken));

    [Description("WRITE ACTION. Retry the card payment of an order in PaymentFailed status. " +
                 "Use only when the user explicitly asks to retry, and only if the runbooks allow a retry for the failure code.")]
    public Task<JsonElement> RetryPayment(
        [Description("Numeric order id.")] int orderId,
        CancellationToken cancellationToken = default)
        => Run("retry_payment", requiresOperator: true, () => ops.RetryPaymentAsync(orderId, cancellationToken));

    // ---------------- plumbing ----------------

    private async Task<JsonElement> Run(string tool, bool requiresOperator, Func<Task<JsonElement>> action)
    {
        var user = User;
        var allowed = ToolCatalog.CanView(user) && (!requiresOperator || ToolCatalog.CanOperate(user));
        if (!allowed)
        {
            logger.LogWarning("DENIED tool {Tool} for {User}", tool, user.Identity?.Name ?? "anonymous");
            AiTelemetry.ToolCalls.Add(1, new("tool", tool), new("outcome", "denied"));
            return Json(new { denied = true, message = $"The signed-in user is not allowed to use {tool}." });
        }

        var result = await action();
        var outcome = result.ValueKind == JsonValueKind.Object && result.TryGetProperty("error", out _) ? "error" : "ok";
        AiTelemetry.ToolCalls.Add(1, new("tool", tool), new("outcome", outcome));

        if (requiresOperator)
            logger.LogInformation("AUDIT AI write tool {Tool} by {User}: {Outcome} {Result}",
                tool, user.Identity?.Name, outcome, result.GetRawText());

        return result;
    }

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
}
