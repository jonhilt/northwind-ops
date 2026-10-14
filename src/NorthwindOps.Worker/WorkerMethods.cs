using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace NorthwindOps.Worker;

public record ProductTotal(string Product, int Units, decimal Revenue);
public record OrderLineTotals(int Lines, ProductTotal[] TopProducts);

[SupportedOSPlatform("browser")]
public static partial class WorkerMethods
{
    // xray:web-worker-template
    // [JSExport] lets the worker's JavaScript call it. Only simple types cross over,
    // so the CSV comes in as bytes and the totals go back as JSON.
    [JSExport]
    public static string TotalOrderLines(byte[] csv)
    // /xray:web-worker-template
    {
        var byProduct = new Dictionary<string, (int Units, decimal Revenue)>();
        using var reader = new StreamReader(new MemoryStream(csv));
        reader.ReadLine(); // skip the header row
        var lines = 0;
        while (reader.ReadLine() is { } line)
        {
            // OrderId,OrderDate,CustomerId,Product,UnitPrice,Quantity,Discount
            var f = line.Split(',');
            var price = decimal.Parse(f[4], CultureInfo.InvariantCulture);
            var quantity = int.Parse(f[5], CultureInfo.InvariantCulture);
            var discount = decimal.Parse(f[6], CultureInfo.InvariantCulture);
            var (units, revenue) = byProduct.GetValueOrDefault(f[3]);
            revenue += price * quantity * (1 - discount);
            byProduct[f[3]] = (units + quantity, revenue);
            lines++;
        }

        var top = byProduct.OrderByDescending(p => p.Value.Revenue).Take(5)
            .Select(p => new ProductTotal(p.Key, p.Value.Units, p.Value.Revenue));
        // xray:web-worker-template
        return JsonSerializer.Serialize(new OrderLineTotals(lines, [.. top]));
        // /xray:web-worker-template
    }
}
