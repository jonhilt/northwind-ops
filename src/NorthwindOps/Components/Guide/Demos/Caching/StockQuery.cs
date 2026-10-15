using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Components.Guide.Demos.Caching;

public record CategoryStock(string Category, int Products, decimal Value);

/// <summary>The slow report both demo pages show: stock value by category.</summary>
public static class StockQuery
{
    public static async Task<List<CategoryStock>> RunAsync(IDbContextFactory<NorthwindDb> dbFactory)
    {
        await Task.Delay(1500); // stands in for a slow reporting query
        await using var db = await dbFactory.CreateDbContextAsync();
        var products = await db.Products.Include(p => p.Category).ToListAsync();
        return [.. products.GroupBy(p => p.Category.Name)
            .Select(g => new CategoryStock(g.Key, g.Count(), g.Sum(p => p.UnitPrice * p.UnitsInStock)))
            .OrderByDescending(r => r.Value)];
    }
}

/// <summary>What each demo page counts: how many report queries it has run.</summary>
public sealed class ReportTally
{
    public static readonly ReportTally Net10 = new(), Net11 = new();

    public int Queries;
}
