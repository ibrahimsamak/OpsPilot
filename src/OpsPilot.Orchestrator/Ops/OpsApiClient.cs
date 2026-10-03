using System.Net.Http.Json;
using System.Text.Json;

namespace OpsPilot.Orchestrator.Ops;

public sealed class OpsApiClient(HttpClient http)
{
    public Task<JsonElement> GetOrderAsync(int orderId, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"orders/{orderId}", null, ct);

    public Task<JsonElement> ListPaymentFailuresAsync(int sinceHours, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"payments/failures?sinceHours={sinceHours}", null, ct);

    public Task<JsonElement> GetStockAsync(string sku, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"inventory/{Uri.EscapeDataString(sku)}", null, ct);

    public Task<JsonElement> ReserveStockAsync(string sku, int quantity, int? orderId, string reason, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"inventory/{Uri.EscapeDataString(sku)}/reservations",
            new { quantity, orderId, reason }, ct);

    public Task<JsonElement> RetryPaymentAsync(int orderId, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"orders/{orderId}/payments/retry", null, ct);

    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode && text.Length > 0)
            return JsonDocument.Parse(text).RootElement.Clone();

        return JsonSerializer.SerializeToElement(new
        {
            error = true,
            status = (int)response.StatusCode,
            detail = text.Length > 500 ? text[..500] : text
        });
    }
}
