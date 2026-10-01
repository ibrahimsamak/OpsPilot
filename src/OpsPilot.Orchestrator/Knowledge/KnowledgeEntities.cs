using Pgvector;

namespace OpsPilot.Orchestrator.Knowledge;

public sealed class KnowledgeDocument
{
    public string Source { get; set; } = "";          // file name, e.g. payment-failure-codes.md
    public string ContentHash { get; set; } = "";     // SHA-256 hex of the file
    public int ChunkCount { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}

public sealed class DocChunk
{
    public long Id { get; set; }
    public string Source { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string Heading { get; set; } = "";
    public string Content { get; set; } = "";
    public Vector? Embedding { get; set; }
}
