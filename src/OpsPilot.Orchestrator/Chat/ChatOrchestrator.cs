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

// D2
// public sealed class ChatOrchestrator(IChatClient chat, KnowledgeSearch knowledge, CitationCollector citations, AiOptions ai)
// {
//     public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, ClaimsPrincipal user, [EnumeratorCancellation] CancellationToken ct)
//     {
//         var question = request.Messages[^1].Content;

//         // 1. Retrieve (always, with the raw question).
//         var hits = await knowledge.SearchAsync(question, ct);
//         yield return ChatEvent.Tool("search_runbooks", new Dictionary<string, object?>
//         {
//             ["query"] = question,
//             ["hits"] = hits.Count
//         });

//         // 2. Prompt: rules in system, sources + question in the last user message.
//         List<ChatMessage> messages =
//         [
//             new(ChatRole.System, Prompts.GroundedRag(user)), .. ChatHistory.ToMessages(request, includeLast: false),
//             new(ChatRole.User, Prompts.WithSources(question, hits))
//         ];

//         var options = new ChatOptions
//         {
//             Temperature = ai.Temperature,
//             MaxOutputTokens = ai.MaxOutputTokens
//         };
//         // 3. Generate (streamed).
//         var answer = new StringBuilder();
//         UsageDetails? usage = null;
//         await foreach (var update in chat.GetStreamingResponseAsync(messages, options, ct))
//         {
//             foreach (var u in update.Contents.OfType<UsageContent>())
//                 (usage ??= new()).Add(u.Details);

//             if (string.IsNullOrEmpty(update.Text)) continue;
//             answer.Append(update.Text);
//             yield return ChatEvent.Delta(update.Text);
//         }
//         // 4. Verify citations.
//         var grounding = citations.Resolve(answer.ToString());
//         if (grounding.UnknownIds.Count > 0)
//             AiTelemetry.UnknownCitations.Add(grounding.UnknownIds.Count);

//         yield return ChatEvent.Grounding(grounding.Citations, grounding.UnknownIds);
//         yield return ChatEvent.Done(usage);


//         // List<ChatMessage> messages = 
//         // [
//         //     new (ChatRole.System, Prompts.Basic(user)),
//         //     .. ChatHistory.ToMessages(request)    
//         // ];

//         // var options = new ChatOptions
//         // {
//         //     Temperature = ai.Temperature,
//         //     MaxOutputTokens = ai.MaxOutputTokens,
//         // };

//         // UsageDetails? usage = null;

//         // await foreach(var update in chat.GetStreamingResponseAsync(messages, options, ct))
//         // {
//         //     foreach(var u in update.Contents.OfType<UsageContent>())
//         //         (usage ??= new()).Add(u.Details);

//         //     if (!string.IsNullOrEmpty(update.Text))
//         //         yield return ChatEvent.Delta(update.Text);
//         // }

//         // yield return ChatEvent.Done(usage);
//     }

// }


// D3
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

        // 3. Generate (streamed).
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
        // 4. Verify citations.
        var grounding = citations.Resolve(answer.ToString());
        if (grounding.UnknownIds.Count > 0)
            AiTelemetry.UnknownCitations.Add(grounding.UnknownIds.Count);

        yield return ChatEvent.Grounding(grounding.Citations, grounding.UnknownIds);
        yield return ChatEvent.Done(usage);


    }

}