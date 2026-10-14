using Microsoft.EntityFrameworkCore;

namespace NorthwindOps.Data;

public class NorthwindDb(DbContextOptions<NorthwindDb> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite has no decimal type and can't ORDER BY one, so money is stored as REAL.
        builder.Properties<decimal>().HaveConversion<double>();
        builder.Properties<OrderStatus>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Order>().HasIndex(o => o.PlacedOn);
        model.Entity<Order>().HasIndex(o => o.Status);

        // Screens keep their own entities in their own files, configured with IEntityTypeConfiguration<T>.
        model.ApplyConfigurationsFromAssembly(typeof(NorthwindDb).Assembly);
    }
}
