namespace OpsPilot.Orchestrator.Knowledge;

public sealed class KnowledgeOptions
{
    public const string Section = "Knowledge";

    public string FolderPath { get; set; } = "../../knowledge";
    public int TopK { get; set; } = 5;
    public double MaxDistance { get; set; } = 0.7;
    public bool IngestOnStartup { get; set; }
}
