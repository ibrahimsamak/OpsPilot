using Microsoft.EntityFrameworkCore;

namespace OpsPilot.Orchestrator.Knowledge;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public const int EmbeddingDimensions = 1536; // text-embedding-3-small

    public DbSet<KnowledgeDocument> Documents => Set<KnowledgeDocument>();
    public DbSet<DocChunk> Chunks => Set<DocChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<KnowledgeDocument>(e =>
        {
            e.ToTable("knowledge_documents");
            e.HasKey(d => d.Source);
            e.Property(d => d.Source).HasMaxLength(200);
            e.Property(d => d.ContentHash).HasMaxLength(64);
        });

        modelBuilder.Entity<DocChunk>(e =>
        {
            e.ToTable("doc_chunks");
            e.HasKey(c => c.Id);
            e.Property(c => c.Source).HasMaxLength(200);
            e.Property(c => c.Heading).HasMaxLength(300);
            e.Property(c => c.Embedding).HasColumnType($"vector({EmbeddingDimensions})");
            e.HasIndex(c => c.Source);
            e.HasIndex(c => c.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        });
    }
}
