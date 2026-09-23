namespace OpsPilot.Orchestrator.AI;

public sealed class AiOptions
{
    public const string Section = "AzureOpenAI";

    public string Endpoint { get; set; } = "";

    public string? ApiKey { get; set; }

    public string ChatDeployment { get; set; } = "chat";
    public string EmbeddingDeployment { get; set; } = "embeddings";

    public float? Temperature { get; set; }

    public int MaxOutputTokens { get; set; } = 1000;
}
