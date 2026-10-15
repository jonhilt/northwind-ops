using System.Globalization;
using System.Text.Json;

namespace NorthwindOps.Worker;

public record ProductTotal(string Product, int Units, decimal Revenue);
public record OrderLineTotals(int Lines, ProductTotal[] TopProducts);

/// <summary>
/// Revenue by product, one CSV line at a time. The worker method feeds it every line in one go;
/// the .NET 10 demo feeds it in a loop that pauses, so both total the same way.
/// </summary>
public sealed class OrderLineTally
{
    readonly Dictionary<string, (int Units, decimal Revenue)> byProduct = [];

    public int Lines { get; private set; }

    // OrderId,OrderDate,CustomerId,Product,UnitPrice,Quantity,Discount
    public void Add(string line)
    {
        var f = line.Split(',');
        var price = decimal.Parse(f[4], CultureInfo.InvariantCulture);
        var quantity = int.Parse(f[5], CultureInfo.InvariantCulture);
        var discount = decimal.Parse(f[6], CultureInfo.InvariantCulture);
        var (units, revenue) = byProduct.GetValueOrDefault(f[3]);
        byProduct[f[3]] = (units + quantity, revenue + price * quantity * (1 - discount));
        Lines++;
    }

    // The top five products, as JSON: a worker can only hand back simple values.
    public string ToJson() => JsonSerializer.Serialize(new OrderLineTotals(Lines, [..
        byProduct.OrderByDescending(p => p.Value.Revenue).Take(5)
            .Select(p => new ProductTotal(p.Key, p.Value.Units, p.Value.Revenue))]));
}
