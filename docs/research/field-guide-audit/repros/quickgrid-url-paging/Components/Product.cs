namespace QuickGridUrlRepro.Components;

public record Product(string Name, decimal UnitPrice)
{
    // 25 products, so 3 pages of 10.
    public static readonly IQueryable<Product> All = Enumerable.Range(1, 25)
        .Select(i => new Product($"Product {i:00}", i * 3m % 50 + 1))
        .AsQueryable();
}
