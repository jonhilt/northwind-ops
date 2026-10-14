namespace NorthwindOps.Data.Seed;

/// <summary>
/// Adds a screen's own demo data after the core Northwind data is in place. Register implementations with
/// <c>services.AddSingleton&lt;IDemoDataSeeder, MySeeder&gt;()</c>; they run on every reset, in registration order.
/// </summary>
public interface IDemoDataSeeder
{
    Task SeedAsync(NorthwindDb db, DateTime today, CancellationToken ct);
}
