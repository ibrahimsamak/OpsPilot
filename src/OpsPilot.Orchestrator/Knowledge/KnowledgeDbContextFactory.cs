using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsPilot.Orchestrator.Knowledge;

public sealed class KnowledgeDbContextFactory : IDesignTimeDbContextFactory<KnowledgeDbContext>
{
    public KnowledgeDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<KnowledgeDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5433;Database=aidb;Username=opspilot;Password=opspilot_dev",
                npgsql => npgsql.UseVector())
            .Options);
}
