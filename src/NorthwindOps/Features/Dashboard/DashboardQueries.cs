using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Features.Dashboard;

/// <summary>The headline numbers across the top of the dashboard.</summary>
public sealed record DashboardKpis(
    DateTimeOffset AsOf,
    int OrdersToday,
    int OrdersYesterday,
    int Pending,
    int Packing,
    int ShippedThisWeek,
    int ShippedLastWeek,
    decimal Revenue,
    decimal RevenueBefore,
    int RevenueOrders)
{
    public int Open => Pending + Packing;

    /// <summary>Revenue for the last 30 days against the 30 days before, as a fraction (0.05 is 5% up).</summary>
    public decimal? RevenueChange => RevenueBefore == 0 ? null : (Revenue - RevenueBefore) / RevenueBefore;
}

public sealed record TopCustomer(string Company, string? City, string? Country, int Orders, decimal Revenue);

public sealed record LowStockProduct(int Id, string Name, string Supplier, int UnitsInStock, int ReorderLevel);

/// <summary>The dashboard's queries. Registered as a singleton: each call opens its own DbContext.</summary>
public sealed class DashboardQueries(IDbContextFactory<NorthwindDb> dbFactory, TimeProvider clock)
{
    /// <summary>
    /// The KPI query stands in for the kind of aggregate that's slow in a real back office: a reporting view
    /// over millions of order lines, or a call to a finance system. The demo database is tiny, so we add the wait.
    /// </summary>
    public static readonly TimeSpan KpiQueryCost = TimeSpan.FromSeconds(1.5);

    int kpiQueryRuns;

    /// <summary>How many times the KPI query has actually run since the app started.</summary>
    public int KpiQueryRuns => Volatile.Read(ref kpiQueryRuns);

    public async Task<DashboardKpis> GetKpisAsync(CancellationToken ct = default)
    {
        Interlocked.Increment(ref kpiQueryRuns);
        await Task.Delay(KpiQueryCost, clock, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.GetLocalNow().Date;
        var weekAgo = today.AddDays(-7);
        var twoWeeksAgo = today.AddDays(-14);
        var monthAgo = today.AddDays(-30);
        var twoMonthsAgo = today.AddDays(-60);

        var placed = await db.Orders
            .Where(o => o.PlacedOn >= twoMonthsAgo && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.PlacedOn, o.Total })
            .ToListAsync(ct);
        var lastMonth = placed.Where(o => o.PlacedOn >= monthAgo).ToList();

        return new DashboardKpis(
            AsOf: clock.GetLocalNow(),
            OrdersToday: placed.Count(o => o.PlacedOn >= today),
            OrdersYesterday: placed.Count(o => o.PlacedOn >= today.AddDays(-1) && o.PlacedOn < today),
            Pending: await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct),
            Packing: await db.Orders.CountAsync(o => o.Status == OrderStatus.Packing, ct),
            ShippedThisWeek: await db.Orders.CountAsync(o => o.ShippedOn >= weekAgo, ct),
            ShippedLastWeek: await db.Orders.CountAsync(o => o.ShippedOn >= twoWeeksAgo && o.ShippedOn < weekAgo, ct),
            Revenue: lastMonth.Sum(o => o.Total),
            RevenueBefore: placed.Where(o => o.PlacedOn < monthAgo).Sum(o => o.Total),
            RevenueOrders: lastMonth.Count);
    }

    public async Task<List<TopCustomer>> GetTopCustomersAsync(int count, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var monthAgo = clock.GetLocalNow().Date.AddDays(-30);

        // SQLite can't SUM a decimal, so group the month's orders in memory: it's a few hundred rows.
        var orders = await db.Orders
            .Where(o => o.PlacedOn >= monthAgo && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.Customer.Company, o.Customer.City, o.Customer.Country, o.Total })
            .ToListAsync(ct);

        return orders
            .GroupBy(o => (o.Company, o.City, o.Country))
            .Select(g => new TopCustomer(g.Key.Company, g.Key.City, g.Key.Country, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(c => c.Revenue)
            .Take(count)
            .ToList();
    }

    public async Task<List<LowStockProduct>> GetLowStockAsync(int count, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Products
            .Where(p => !p.Discontinued && p.UnitsInStock <= p.ReorderLevel)
            .OrderBy(p => p.UnitsInStock - p.ReorderLevel)
            .ThenBy(p => p.Name)
            .Take(count)
            .Select(p => new LowStockProduct(p.Id, p.Name, p.Supplier.Company, p.UnitsInStock, p.ReorderLevel))
            .ToListAsync(ct);
    }
}
