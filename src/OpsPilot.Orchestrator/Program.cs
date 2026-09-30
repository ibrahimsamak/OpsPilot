using OpsPilot.Orchestrator.AI;
using OpsPilot.Orchestrator.Chat;
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

builder.Services.AddScoped<ChatOrchestrator>();

var app = builder.Build();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapChatEndpoints();


app.Run();
