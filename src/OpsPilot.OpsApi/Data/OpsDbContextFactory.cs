using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsPilot.OpsApi.Data;

public sealed class OpsDbContextFactory : IDesignTimeDbContextFactory<OpsDbContext>
{
    public OpsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OpsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=opsdb;Username=opspilot;Password=opspilot_dev")
            .Options);
}
