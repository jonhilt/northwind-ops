using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Features.Dashboard;

public enum OrderEventKind
{
    NewOrder,
    Packed,
    Shipped,
    StockAlert,
}

/// <summary>Something that just happened in the warehouse.</summary>
public sealed record OrderEvent(long Id, DateTimeOffset At, OrderEventKind Kind, string Subject, string Detail, decimal? Amount = null);

/// <summary>
/// The warehouse's live event stream. In a real system this would be fed by a message queue; here a
/// simulator raises a plausible event every few seconds. Every visitor sees the same stream.
/// </summary>
public sealed class OrderFeed(IDbContextFactory<NorthwindDb> dbFactory, TimeProvider clock, ILogger<OrderFeed> logger) : BackgroundService
{
    /// <summary>How much recent history to make up on start, so the feed isn't empty for the first visitor.</summary>
    public const int Backfill = 30;

    /// <summary>Old events are trimmed in one go once there are this many (roughly a day's worth).</summary>
    const int Capacity = 20_000;

    readonly List<OrderEvent> events = [];
    readonly Lock gate = new();

    /// <summary>Raised on a background thread after each new event is stored.</summary>
    public event Action<OrderEvent>? EventAdded;

    public int Count
    {
        get { lock (gate) { return events.Count; } }
    }

    /// <summary>Reads a slice of the feed, oldest first.</summary>
    public IReadOnlyList<OrderEvent> Read(int start, int count)
    {
        lock (gate)
        {
            start = Math.Clamp(start, 0, events.Count);
            return events.GetRange(start, Math.Clamp(count, 0, events.Count - start));
        }
    }

    public void Add(OrderEvent orderEvent)
    {
        lock (gate)
        {
            if (events.Count >= Capacity)
            {
                events.RemoveRange(0, Capacity / 2);
            }
            events.Add(orderEvent);
        }
        EventAdded?.Invoke(orderEvent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var simulator = new OrderFeedSimulator(await LoadCatalogAsync(stoppingToken), new Random());

            var now = clock.GetLocalNow();
            for (var i = Backfill; i > 0; i--)
            {
                Add(simulator.Next(now.AddSeconds(-i * 17)));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(2_000, 5_000)), clock, stoppingToken);
                Add(simulator.Next(clock.GetLocalNow()));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The order feed simulator stopped");
        }
    }

    async Task<FeedCatalog> LoadCatalogAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var customers = await db.Customers.Select(c => c.City == null ? c.Company : c.Company + ", " + c.City).ToListAsync(ct);
        var products = await db.Products.Where(p => !p.Discontinued)
            .Select(p => new FeedProduct(p.Name, p.UnitPrice, p.UnitsInStock, p.ReorderLevel))
            .ToListAsync(ct);
        var lastOrderId = await db.Orders.MaxAsync(o => (int?)o.Id, ct) ?? 11077;
        var openOrders = await db.Orders
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Packing)
            .OrderBy(o => o.PlacedOn)
            .Select(o => new { o.Id, o.Status })
            .ToListAsync(ct);

        return new FeedCatalog(
            customers,
            products,
            NextOrderId: lastOrderId + 1,
            Pending: openOrders.Where(o => o.Status == OrderStatus.Pending).Select(o => o.Id).ToList(),
            Packing: openOrders.Where(o => o.Status == OrderStatus.Packing).Select(o => o.Id).ToList());
    }
}

public sealed record FeedProduct(string Name, decimal UnitPrice, int UnitsInStock, int ReorderLevel);

public sealed record FeedCatalog(IReadOnlyList<string> Customers, IReadOnlyList<FeedProduct> Products, int NextOrderId, IReadOnlyList<int> Pending, IReadOnlyList<int> Packing);

/// <summary>
/// Makes up warehouse events that hang together: orders are placed, then packed, then shipped, and picking
/// stock for them now and then takes a product under its reorder level.
/// </summary>
public sealed class OrderFeedSimulator(FeedCatalog catalog, Random random)
{
    static readonly string[] Carriers = ["Speedy Express", "United Package", "Federal Shipping"];

    readonly Queue<int> pending = new(catalog.Pending);
    readonly Queue<(int Id, int Lines)> packing = new(catalog.Packing.Select(id => (id, 0)));
    readonly Dictionary<string, int> stock = catalog.Products.ToDictionary(p => p.Name, p => p.UnitsInStock);
    readonly Dictionary<int, int> linesByOrder = [];
    int nextOrderId = catalog.NextOrderId;
    long nextEventId = 1;

    public OrderEvent Next(DateTimeOffset at)
    {
        var roll = random.Next(100);
        var (kind, subject, detail, amount) = roll switch
        {
            < 12 when TryStockAlert() is { } alert => alert,
            < 45 when packing.Count > 0 => Ship(),
            < 75 when pending.Count > 0 => Pack(),
            _ => PlaceOrder(),
        };
        return new OrderEvent(nextEventId++, at, kind, subject, detail, amount);
    }

    (OrderEventKind, string, string, decimal?) PlaceOrder()
    {
        var id = nextOrderId++;
        var lines = random.Next(1, 6);
        var total = 0m;
        for (var i = 0; i < lines; i++)
        {
            var product = catalog.Products[random.Next(catalog.Products.Count)];
            total += product.UnitPrice * random.Next(1, 13) * 5;
        }
        pending.Enqueue(id);
        linesByOrder[id] = lines;
        var customer = catalog.Customers[random.Next(catalog.Customers.Count)];
        return (OrderEventKind.NewOrder, $"Order #{id}", $"{customer} · {lines} {(lines == 1 ? "line" : "lines")}", Math.Round(total, 2));
    }

    (OrderEventKind, string, string, decimal?) Pack()
    {
        var id = pending.Dequeue();
        var lines = linesByOrder.GetValueOrDefault(id, random.Next(1, 6));
        packing.Enqueue((id, lines));
        var picker = random.Next(1, 7);
        return (OrderEventKind.Packed, $"Order #{id}", $"Packed at bay {picker} · {lines} {(lines == 1 ? "line" : "lines")} picked", null);
    }

    (OrderEventKind, string, string, decimal?) Ship()
    {
        var (id, _) = packing.Dequeue();
        linesByOrder.Remove(id);
        var carrier = Carriers[random.Next(Carriers.Length)];
        return (OrderEventKind.Shipped, $"Order #{id}", $"Collected by {carrier}", null);
    }

    (OrderEventKind, string, string, decimal?)? TryStockAlert()
    {
        // Picking uses up stock; the alert fires once a product drops to its reorder level.
        var product = catalog.Products[random.Next(catalog.Products.Count)];
        if (product.ReorderLevel == 0)
        {
            return null;
        }
        var left = stock[product.Name] = Math.Max(0, Math.Min(stock[product.Name], product.ReorderLevel) - random.Next(1, 6));
        var detail = left == 0
            ? $"Out of stock · reorder level {product.ReorderLevel}"
            : $"{left} units left · reorder level {product.ReorderLevel}";
        return (OrderEventKind.StockAlert, product.Name, detail, null);
    }
}
