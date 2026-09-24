namespace OpsPilot.Orchestrator.Chat;
public static class InputGuard
{
    public const int MaxMessages = 40;
    public const int MaxQuestionChars = 2000;
    public const int MaxMessageChars = 12000;


    private static readonly string[] InjectionMarkers =
    [
        "ignore previous instructions",
        "ignore all previous",
        "ignore the above",
        "disregard the system prompt",
        "reveal your system prompt",
        "print your system prompt",
        "you are now",
        "developer mode",
    ];

    public static string? Validate(ChatRequest? request)
    {
        if(request?.Messages is not {Count: > 0 } messages)
            return "At least one message is required.";

        if(messages.Count > MaxMessages)
            return $"Too many messages (max {MaxMessages}). Start a new conversation.";

        if (messages.Any(m => m.Role is not ("user" or "assistant")))
            return "Message role must be 'user' or 'assistant'.";
        
        if (messages.Any(m => string.IsNullOrWhiteSpace(m.Content) || m.Content.Length > MaxMessageChars))
            return $"Messages must be non-empty and at most {MaxMessageChars} characters.";

        var last = messages[^1];
        if (last.Role != "user")
            return "The last message must be from the user.";

        if (last.Content.Length > MaxQuestionChars)
            return $"Question too long (max {MaxQuestionChars} characters).";

        return null;
    }

    public static bool LooksLikeInjection(string text) => InjectionMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
}