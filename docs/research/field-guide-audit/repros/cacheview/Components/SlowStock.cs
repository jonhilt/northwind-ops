namespace CacheViewRepro.Components;
public record CategoryStock(string Category, int Products, decimal Value);
public static class SlowStock
{
    public static async Task<List<CategoryStock>> LoadAsync()
    {
        Interlocked.Increment(ref Stats.QueriesRun);
        await Task.Delay(1500); // stands in for a slow reporting query
        return [new("Beverages", 12, 12480m), new("Condiments", 12, 12023m), new("Seafood", 12, 13010m)];
    }
}
