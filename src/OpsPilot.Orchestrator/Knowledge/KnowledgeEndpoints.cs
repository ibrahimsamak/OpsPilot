using Microsoft.EntityFrameworkCore;
using OpsPilot.ServiceDefaults;

namespace OpsPilot.Orchestrator.Knowledge;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ingest", async (IngestionService ingestion, CancellationToken ct) =>
                Results.Ok(await ingestion.IngestFolderAsync(ct)))
            .RequireAuthorization(OpsPolicies.CanAdmin);

        app.MapGet("/api/knowledge/documents", async (KnowledgeDbContext db, CancellationToken ct) =>
                Results.Ok(await db.Documents.AsNoTracking().OrderBy(d => d.Source).ToListAsync(ct)))
            .RequireAuthorization(OpsPolicies.CanView);

        app.MapGet("/api/knowledge/search", async (string q, KnowledgeSearch search, CancellationToken ct) =>
                Results.Ok(await search.SearchAsync(q, ct, applyThreshold: false)))
            .RequireAuthorization(OpsPolicies.CanView);

        return app;
    }
}
