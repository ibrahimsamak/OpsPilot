using Microsoft.EntityFrameworkCore;
using OpsPilot.OpsApi.Data;
using OpsPilot.OpsApi.Endpoints;
using OpsPilot.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.AddOpsAuth();

builder.Services.AddDbContext<OpsDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("OpsDb")));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // http://localhost:5101/openapi/v1.json

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OpsDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(db);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapOrderEndpoints();
app.MapPaymentEndpoints();
app.MapInventoryEndpoints();

app.Run();
