using System.Globalization;
using System.Text;

namespace WorkerRepro;

public record ProductTotal(string Product, int Units, decimal Revenue);

// The same shape as the guide demo: a CSV export of order lines, totalled by product.
public static class OrderLines
{
    public const string Header = "OrderId,OrderDate,CustomerId,Product,UnitPrice,Quantity,Discount";
    static readonly string[] Products = ["Chai", "Chang", "Aniseed Syrup", "Ikura", "Tofu", "Konbu", "Pavlova", "Geitost", "Ipoh Coffee", "Gnocchi"];

    public static byte[] Build(int lines)
    {
        var rng = new Random(42);
        var sb = new StringBuilder(Header).Append('\n');
        for (var i = 0; i < lines; i++)
        {
            var p = rng.Next(Products.Length);
            sb.Append(10248 + i).Append(",2025-")
              .Append((i % 12 + 1).ToString("00")).Append("-01,ALFKI,")
              .Append(Products[p]).Append(',')
              .Append((10 + p * 3.5m).ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(rng.Next(1, 50)).Append(',')
              .Append(rng.Next(5) == 0 ? "0.1" : "0").Append('\n');
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // .NET 10: the only place this can run in a Blazor WebAssembly app is the page's one UI thread.
    public static async Task<(int Lines, ProductTotal[] Top)> Total(byte[] csv, int yieldEvery = 0)
    {
        var byProduct = new Dictionary<string, (int Units, decimal Revenue)>();
        using var reader = new StreamReader(new MemoryStream(csv));
        reader.ReadLine();
        var lines = 0;
        while (reader.ReadLine() is { } line)
        {
            var f = line.Split(',');
            var price = decimal.Parse(f[4], CultureInfo.InvariantCulture);
            var quantity = int.Parse(f[5], CultureInfo.InvariantCulture);
            var discount = decimal.Parse(f[6], CultureInfo.InvariantCulture);
            var (units, revenue) = byProduct.GetValueOrDefault(f[3]);
            byProduct[f[3]] = (units + quantity, revenue + price * quantity * (1 - discount));
            lines++;
            // Workaround 1 from the guide: pause every so often so the page can draw.
            if (yieldEvery > 0 && lines % yieldEvery == 0)
                await Task.Delay(1);
        }
        var top = byProduct.OrderByDescending(p => p.Value.Revenue).Take(5)
            .Select(p => new ProductTotal(p.Key, p.Value.Units, p.Value.Revenue)).ToArray();
        return (lines, top);
    }
}
