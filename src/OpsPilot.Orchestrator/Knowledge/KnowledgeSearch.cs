using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace OpsPilot.Orchestrator.Knowledge;

public sealed record KnowledgeHit(string Id, string Source, string Heading, string Content, double Score);


public sealed class KnowledgeSearch(
    KnowledgeDbContext db,
    IEmbeddingGenerator<string, Embedding<float>> embedder,
    CitationCollector citations,
    IOptions<KnowledgeOptions> options)
{
    /// <param name="applyThreshold">false = return the raw top-k (debug endpoint, tuning).</param>
    public async Task<IReadOnlyList<KnowledgeHit>> SearchAsync(string query, CancellationToken ct, bool applyThreshold = true)
    {
        var settings = options.Value;
        var queryVector = new Vector(await embedder.GenerateVectorAsync(query, cancellationToken: ct));
        var rows = await db.Chunks
            .AsNoTracking()
            .Select(c => new
            {
                c.Source,
                c.ChunkIndex,
                c.Heading,
                c.Content,
                Distance = c.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(settings.TopK)
            .ToListAsync(ct);
        var hits = rows
            .Where(r => !applyThreshold || r.Distance <= settings.MaxDistance)
            .Select(r => new KnowledgeHit(
                Id: $"{r.Source}#{r.ChunkIndex}",
                Source: r.Source,
                Heading: r.Heading,
                Content: r.Content,
                Score: Math.Round(1 - r.Distance, 3)))
            .ToList();
        citations.Register(hits);
        return hits;
    }
}