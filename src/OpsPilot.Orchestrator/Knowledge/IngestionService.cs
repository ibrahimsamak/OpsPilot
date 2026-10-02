using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pgvector;

namespace OpsPilot.Orchestrator.Knowledge;

public sealed record IngestionReport(int Files, int Updated, int Unchanged, int Removed, int ChunksWritten);


public sealed class IngestionService(
    KnowledgeDbContext db,
    IEmbeddingGenerator<string, Embedding<float>> embedder,
    IOptions<KnowledgeOptions> options,
    IHostEnvironment env,
    ILogger<IngestionService> logger)
{
    private const int EmbeddingBatchSize = 64;
    public async Task<IngestionReport> IngestFolderAsync(CancellationToken ct)
    {
        var folder = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.FolderPath));
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"Knowledge folder not found: {folder}");

        var files = Directory.GetFiles(folder, "*.md", SearchOption.TopDirectoryOnly).OrderBy(f => f).ToArray();
        var existing = await db.Documents.ToDictionaryAsync(d => d.Source, ct);
        int updated = 0, unchanged = 0, chunksWritten = 0;
        foreach (var file in files)
        {
            var source = Path.GetFileName(file);
            var text = await File.ReadAllTextAsync(file, ct);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            if (existing.TryGetValue(source, out var doc) && doc.ContentHash == hash)
            {
                unchanged++;
                continue;
            }
            var chunks = MarkdownChunker.Split(text);
            var vectors = new List<Embedding<float>>(chunks.Count);
            foreach(var batch in chunks.Chunk(EmbeddingBatchSize))
            {
                vectors.AddRange(await embedder.GenerateAsync(batch.Select(c=> EmbeddingInput(source, c)), cancellationToken:ct));
            }
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Chunks.Where(x=>x.Source == source).ExecuteDeleteAsync(ct);
            db.Chunks.AddRange(chunks.Select((c,i)=> new DocChunk
            {
                Source = source,
                ChunkIndex = c.Index,
                Heading= c.Heading,
                Content = c.Content,
                Embedding = new Vector(vectors[i].Vector)
            }));

            if (doc is null)
            {
                doc = new KnowledgeDocument { Source = source };
                db.Documents.Add(doc);
            }

            doc.ContentHash = hash;
            doc.ChunkCount = chunks.Count;
            doc.IngestedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            updated++;
            chunksWritten += chunks.Count;
            logger.LogInformation("Ingested {Source}: {Chunks} chunks", source, chunks.Count);
        }

        var present = files.Select(f => Path.GetFileName(f)).ToHashSet();
        var removed = existing.Keys.Where(source => !present.Contains(source)).ToList();
        foreach (var source in removed)
        {
            await db.Chunks.Where(c => c.Source == source).ExecuteDeleteAsync(ct);
            db.Documents.Remove(existing[source]);
        }
        await db.SaveChangesAsync(ct);

        return new IngestionReport(files.Length, updated, unchanged, removed.Count, chunksWritten);
    }
    public static string EmbeddingInput(string source, TextChunk chunk) => $"{source} | {chunk.Heading}\n{chunk.Content}";
}