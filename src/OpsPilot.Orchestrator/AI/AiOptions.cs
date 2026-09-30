namespace OpsPilot.Orchestrator.AI;

public sealed class AiOptions
{
    public const string Section = "AzureOpenAI";

    /// <summary>Resource endpoint, e.g. https://aoai-opspilot-xx.openai.azure.com/</summary>
    public string Endpoint { get; set; } = "";

    /// <summary>Leave empty to use keyless auth (DefaultAzureCredential).</summary>
    public string? ApiKey { get; set; }

    public string ChatDeployment { get; set; } = "chat";
    public string EmbeddingDeployment { get; set; } = "embeddings";

    /// <summary>Null = don't send (required for reasoning models).</summary>
    public float? Temperature { get; set; }

    public int MaxOutputTokens { get; set; } = 1000;
}
