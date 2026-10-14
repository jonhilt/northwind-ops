using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NorthwindOps.Data.Seed;

/// <summary>
/// Rebuilds the demo database. Customers, suppliers, products and categories come from
/// Microsoft's Northwind sample (MIT licensed, see northwind.json). Orders are generated
/// deterministically and dated relative to today, so the data always looks current.
/// </summary>
public static class NorthwindSeeder
{
    const int FirstOrderId = 11078; // carries on from the last order in the original Northwind

    public static async Task ResetAsync(NorthwindDb db, DateTime today, IEnumerable<IDemoDataSeeder>? screens = null, CancellationToken ct = default)
    {
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);

        var source = await LoadAsync(ct);
        db.Categories.AddRange(source.Categories.Select(c => new Category { Id = c.Id, Name = c.Name, Description = c.Description }));
        db.Suppliers.AddRange(source.Suppliers.Select(s => new Supplier { Id = s.Id, Company = s.Company, Contact = s.Contact, City = s.City, Country = s.Country, Phone = s.Phone }));
        db.Customers.AddRange(source.Customers.Select(c => new Customer { Id = c.Id, Company = c.Company, Contact = c.Contact, City = c.City, Country = c.Country }));
        var products = source.Products.Select(p => new Product
        {
            Id = p.Id, Name = p.Name, SupplierId = p.SupplierId, CategoryId = p.CategoryId, QuantityPerUnit = p.QuantityPerUnit,
            UnitPrice = p.UnitPrice, UnitsInStock = p.UnitsInStock, ReorderLevel = p.ReorderLevel, Discontinued = p.Discontinued,
        }).ToList();
        db.Products.AddRange(products);
        db.Orders.AddRange(GenerateOrders(source.Customers.Select(c => c.Id).ToList(), products.Where(p => !p.Discontinued).ToList(), today));

        await db.SaveChangesAsync(ct);

        // Screens with data of their own (tickets, purchase orders...) add it on top of the core Northwind data.
        foreach (var screen in screens ?? [])
        {
            await screen.SeedAsync(db, today, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    static IEnumerable<Order> GenerateOrders(List<string> customerIds, List<Product> products, DateTime today)
    {
        var random = new Random(1102);
        var id = FirstOrderId;

        // Roughly 4 orders a day for the past year, busier on weekdays.
        for (var daysAgo = 365; daysAgo >= 0; daysAgo--)
        {
            var day = today.AddDays(-daysAgo);
            var count = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? random.Next(0, 3) : random.Next(2, 7);
            for (var i = 0; i < count; i++)
            {
                var placed = day.AddHours(random.Next(7, 19)).AddMinutes(random.Next(60));
                var order = new Order { Id = id++, CustomerId = customerIds[random.Next(customerIds.Count)], PlacedOn = placed };

                foreach (var product in products.OrderBy(_ => random.Next()).Take(random.Next(1, 6)))
                {
                    order.Lines.Add(new OrderLine
                    {
                        ProductId = product.Id,
                        UnitPrice = product.UnitPrice,
                        Quantity = random.Next(1, 13) * 5,
                        Discount = random.Next(10) < 7 ? 0m : 0.05m * random.Next(1, 5),
                    });
                }

                order.Total = Math.Round(order.Lines.Sum(l => l.UnitPrice * l.Quantity * (1 - l.Discount)), 2);
                (order.Status, order.ShippedOn) = daysAgo switch
                {
                    0 => (random.Next(3) == 0 ? OrderStatus.Packing : OrderStatus.Pending, (DateTime?)null),
                    <= 2 => (OrderStatus.Packing, null),
                    <= 7 => (OrderStatus.Shipped, placed.AddDays(random.Next(1, 3))),
                    _ when random.Next(40) == 0 => (OrderStatus.Cancelled, null),
                    _ => (OrderStatus.Delivered, placed.AddDays(random.Next(1, 4))),
                };
                yield return order;
            }
        }
    }

    static async Task<SeedData> LoadAsync(CancellationToken ct)
    {
        await using var stream = typeof(NorthwindSeeder).Assembly.GetManifestResourceStream("NorthwindOps.Data.Seed.northwind.json")
            ?? throw new InvalidOperationException("Seed data resource is missing.");
        return await JsonSerializer.DeserializeAsync<SeedData>(stream, JsonSerializerOptions.Web, ct)
            ?? throw new InvalidOperationException("Seed data is empty.");
    }

    record SeedData(List<SeedCategory> Categories, List<SeedSupplier> Suppliers, List<SeedProduct> Products, List<SeedCustomer> Customers);
    record SeedCategory(int Id, string Name, string? Description);
    record SeedSupplier(int Id, string Company, string? Contact, string? City, string? Country, string? Phone);
    record SeedProduct(int Id, string Name, int SupplierId, int CategoryId, string? QuantityPerUnit, decimal UnitPrice, int UnitsInStock, int ReorderLevel, bool Discontinued);
    record SeedCustomer(string Id, string Company, string? Contact, string? City, string? Country);
}
