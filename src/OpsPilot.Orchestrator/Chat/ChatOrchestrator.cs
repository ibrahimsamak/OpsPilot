using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.Extensions.AI;
using OpsPilot.Orchestrator.AI;
using OpsPilot.Orchestrator.Knowledge;
using OpsPilot.Orchestrator.Telemetry;
using OpsPilot.Orchestrator.Tools;

namespace OpsPilot.Orchestrator.Chat;
















public sealed class ChatOrchestrator(
    IChatClient chat,
    OpsTools tools,
    CitationCollector citations,
    AiOptions ai)
{
    public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, ClaimsPrincipal user, [EnumeratorCancellation] CancellationToken ct)
    {
        var available = ToolCatalog.For(user, tools);
        List<ChatMessage> messages =
        [
                   new(ChatRole.System, Prompts.Agent(user, available.Select(t => t.Name))),.. ChatHistory.ToMessages(request)
        ];

        var options = new ChatOptions
        {
            Tools = [.. available],
            ToolMode = ChatToolMode.Auto,
            Temperature = ai.Temperature,
            MaxOutputTokens = ai.MaxOutputTokens
        };

        var answer = new StringBuilder();
        UsageDetails? usage = null;
        await foreach (var update in chat.GetStreamingResponseAsync(messages, options, ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent call)
                    yield return ChatEvent.Tool(call.Name, call.Arguments);

                else if (content is UsageContent u)
                    (usage ??= new()).Add(u.Details);
            }
            if (string.IsNullOrEmpty(update.Text)) continue;
            answer.Append(update.Text);
            yield return ChatEvent.Delta(update.Text);
        }
        var grounding = citations.Resolve(answer.ToString());
        if (grounding.UnknownIds.Count > 0)
            AiTelemetry.UnknownCitations.Add(grounding.UnknownIds.Count);

        yield return ChatEvent.Grounding(grounding.Citations, grounding.UnknownIds);
        yield return ChatEvent.Done(usage);


    }

}