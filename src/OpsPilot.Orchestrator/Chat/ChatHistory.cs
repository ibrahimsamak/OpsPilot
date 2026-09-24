using Microsoft.Extensions.AI;

namespace OpsPilot.Orchestrator.Chat;
public static class ChatHistory
{
    public const int MaxMessagesKept = 12;

    public static IEnumerable<ChatMessage> ToMessages(ChatRequest request, bool includeLast = true)
    {
        var messages = request.Messages.TakeLast(MaxMessagesKept).ToList();
        if(!includeLast) messages.RemoveAt(messages.Count - 1);

        return messages.Select(m => new ChatMessage(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Content));
    }
}