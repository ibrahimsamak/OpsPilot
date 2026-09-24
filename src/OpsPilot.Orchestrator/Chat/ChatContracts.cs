
using Microsoft.Extensions.AI;

namespace OpsPilot.Orchestrator.Chat;

public sealed record ChatMessageDto(string Role, string Content);

public sealed record ChatRequest(List<ChatMessageDto> Messages);

public sealed record Citation(string Id, string Source, string Heading, string Snippet);

public sealed record ChatEvent(string Type, object Data)
{
    public static ChatEvent Delta(string text) => new("delta", new {text});
    public static ChatEvent Tool(string name, IDictionary<string, object>? arguments)=> new ("tool", new{name, arguments});

    public static ChatEvent Grounding(IReadOnlyList<Citation> citations, IReadOnlyList<string> unknownCitations) => new("grounding", new { citations, unknownCitations });
    public static ChatEvent Done(UsageDetails? usage) => new("done", new { inputTokens = usage?.InputTokenCount, outputTokens = usage?.OutputTokenCount });
    public static ChatEvent Error(string message) => new("error", new { message });
}