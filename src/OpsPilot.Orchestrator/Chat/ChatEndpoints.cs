
using System.ClientModel;
using System.Diagnostics;
using System.Security.Claims;
using OpsPilot.Orchestrator.Telemetry;
using OpsPilot.Orchestrator.Tools;
using OpsPilot.ServiceDefaults;


namespace OpsPilot.Orchestrator.Chat;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            name = user?.Identity?.Name,
            roles = user?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray(),
            tools = ToolCatalog.ToolNamesFor(user)

        }))
        .RequireAuthorization(OpsPolicies.CanView);

        app.MapPost("/api/chat", async (
            ChatRequest request,
            HttpContext http,
            ChatOrchestrator orchestrator,
            ILogger<ChatOrchestrator> logger,
            CancellationToken ct) =>
        {
            if (InputGuard.Validate(request) is { } problem)
                return Results.BadRequest(new { error = problem });

            var question = request.Messages[^1].Content;
            if (InputGuard.LooksLikeInjection(question))
            {
                logger.LogWarning("Suspected prompt injection from {User}", http.User.Identity?.Name);
                Activity.Current?.SetTag("opspilot.guard.suspected_injection", true);
                AiTelemetry.SuspectedInjections.Add(1);
            }

            Sse.Start(http);
            try
            {
                await foreach (var evt in orchestrator.StreamAsync(request, http.User, ct))
                    await Sse.WriteAsync(http.Response, evt, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // The user pressed Stop or closed the tab. Nothing to send.
            }
            catch (ClientResultException ex) when (ex.Status == 400 && ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(ex, "Azure OpenAI content filter blocked a request");
                await Sse.WriteAsync(http.Response, ChatEvent.Error("The request was blocked by the Azure OpenAI content filter."), CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chat request failed");
                await Sse.WriteAsync(http.Response, ChatEvent.Error($"The assistant failed to answer. Trace id: {Activity.Current?.TraceId}"), CancellationToken.None);
            }

            return Results.Empty;
        })
        .RequireAuthorization(OpsPolicies.CanView);



        return app;
    }
}