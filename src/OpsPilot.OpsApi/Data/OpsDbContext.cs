using Microsoft.EntityFrameworkCore;

namespace OpsPilot.OpsApi.Data;

public sealed class OpsDbContext(DbContextOptions<OpsDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<StockReservation> Reservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Sku);
            e.Property(p => p.Sku).HasMaxLength(32);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Price).HasPrecision(12, 2);
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).ValueGeneratedNever();
            e.Property(o => o.CustomerRef).HasMaxLength(32);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.Total).HasPrecision(12, 2);
            e.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
            e.HasMany(o => o.Payments).WithOne().HasForeignKey(p => p.OrderId);
            e.HasIndex(o => o.CreatedAt);
        });

        b.Entity<OrderLine>(e =>
        {
            e.ToTable("order_lines");
            e.Property(l => l.Sku).HasMaxLength(32);
            e.Property(l => l.UnitPrice).HasPrecision(12, 2);
        });

        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Amount).HasPrecision(12, 2);
            e.Property(p => p.Provider).HasMaxLength(40);
            e.Property(p => p.FailureCode).HasMaxLength(40);
            e.Property(p => p.FailureMessage).HasMaxLength(300);
            e.HasIndex(p => new { p.Status, p.AttemptedAt });
        });

        b.Entity<StockReservation>(e =>
        {
            e.ToTable("stock_reservations");
            e.Property(r => r.Sku).HasMaxLength(32);
            e.Property(r => r.Reason).HasMaxLength(300);
            e.Property(r => r.CreatedBy).HasMaxLength(100);
            e.HasIndex(r => r.Sku);
        });
    }
}
