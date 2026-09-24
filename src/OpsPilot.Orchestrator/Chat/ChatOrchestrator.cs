using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.Extensions.AI;
using OpsPilot.Orchestrator.AI;

namespace OpsPilot.Orchestrator.Chat;

public sealed class ChatOrchestrator(IChatClient chat, AiOptions ai)
{
    public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, ClaimsPrincipal user, [EnumeratorCancellation] CancellationToken ct)
    {
        List<ChatMessage> messages = 
        [
            new (ChatRole.System, Prompts.Basic(user)),
            .. ChatHistory.ToMessages(request)    
        ];

        var options = new ChatOptions
        {
            Temperature = ai.Temperature,
            MaxOutputTokens = ai.MaxOutputTokens,
        };

        UsageDetails? usage = null;

        await foreach(var update in chat.GetStreamingResponseAsync(messages, options, ct))
        {
            foreach(var u in update.Contents.OfType<UsageContent>())
                (usage ??= new()).Add(u.Details);
            
            if (!string.IsNullOrEmpty(update.Text))
                yield return ChatEvent.Delta(update.Text);
        }

        yield return ChatEvent.Done(usage);
    }

}