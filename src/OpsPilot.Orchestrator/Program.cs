using Microsoft.EntityFrameworkCore;
using OpsPilot.Orchestrator.AI;
using OpsPilot.Orchestrator.Chat;
using OpsPilot.Orchestrator.Knowledge;
using OpsPilot.Orchestrator.Telemetry;
using OpsPilot.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability(AiTelemetry.SourceName);
builder.AddOpsAuth();
builder.AddAI();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(o => o.AddPolicy("web", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:4200" })
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Knowledge / RAG
builder.Services.Configure<KnowledgeOptions>(builder.Configuration.GetSection(KnowledgeOptions.Section));
builder.Services.AddDbContext<KnowledgeDbContext>(o => o.UseNpgsql(
    builder.Configuration.GetConnectionString("KnowledgeDb"),
    npgsql => npgsql.UseVector()));
builder.Services.AddScoped<CitationCollector>();
builder.Services.AddScoped<KnowledgeSearch>();
builder.Services.AddScoped<IngestionService>();

builder.Services.AddScoped<ChatOrchestrator>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>().Database.MigrateAsync();

    if (app.Configuration.GetValue<bool>("Knowledge:IngestOnStartup"))
    {
        var report = await scope.ServiceProvider.GetRequiredService<IngestionService>().IngestFolderAsync(CancellationToken.None);
        app.Logger.LogInformation("Knowledge ingestion: {@Report}", report);
    }
}

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapChatEndpoints();
app.MapKnowledgeEndpoints();

app.Run();