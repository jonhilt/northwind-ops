using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthwindOps.Data.Seed;

namespace NorthwindOps.Data;

/// <summary>The order detail and edit screen keeps an internal note on each order.</summary>
public sealed class OrderNotesConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> order) => order.Property(o => o.Notes).HasMaxLength(500);
}

/// <summary>Puts the kind of notes a warehouse team really leaves on a handful of recent orders.</summary>
public sealed class OrderNotesSeeder : IDemoDataSeeder
{
    static readonly string[] Notes =
    [
        "Customer asked for delivery before 10am: their loading bay closes at noon.",
        "Called to confirm the delivery address; they've moved to the unit next door.",
        "Two cases of the cheese were short-dated. Swapped before packing.",
        "Pallet wrapped twice; last delivery arrived with a crushed corner.",
        "Account manager agreed free delivery on this one after the late order last month.",
        "Back-ordered item will follow separately. Customer is happy to receive in two drops.",
    ];

    public async Task SeedAsync(NorthwindDb db, DateTime today, CancellationToken ct)
    {
        var recent = await db.Orders
            .Where(o => o.PlacedOn >= today.AddDays(-21))
            .OrderBy(o => o.Id)
            .ToListAsync(ct);

        var random = new Random(11076);
        foreach (var order in recent.Where(_ => random.Next(4) == 0))
        {
            order.Notes = Notes[random.Next(Notes.Length)];
        }
    }
}
