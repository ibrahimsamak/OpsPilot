using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace OpsPilot.Orchestrator.Chat;

public static class Sse
{
    public static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static void Start(HttpContext http)
    {
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers["X-Accel-Buffering"] = "no"; // tell reverse proxies not to buffer
        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
    }

    public static async Task WriteAsync(HttpResponse response, ChatEvent evt, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(evt.Data, Json);
        await response.WriteAsync($"event: {evt.Type}\ndata: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}