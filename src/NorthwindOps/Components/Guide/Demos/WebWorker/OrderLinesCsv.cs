using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Components.Guide.Demos.WebWorker;

/// <summary>
/// The CSV export the Web Worker demo totals: a year of made-up order lines for Northwind's products,
/// the same every time. It's sized so totalling it takes two or three seconds in the browser, long enough to
/// see the clock stop on the UI thread. It's built once, on the first request, and kept gzipped, so every
/// visitor shares one copy.
/// </summary>
public static class OrderLinesCsv
{
    // A Debug build's WebAssembly runs with the debugger enabled, several times slower than a published one,
    // so each gets as many lines as it can total in about the same time.
#if DEBUG
    public const int Lines = 80_000;
#else
    public const int Lines = 500_000; // about 3.4 MB gzipped: still a visible freeze, light enough for conference wifi
#endif
    public const string Header = "OrderId,OrderDate,CustomerId,Product,UnitPrice,Quantity,Discount";

    static byte[]? gzipped;

    public static async Task GetAsync(HttpContext context, IDbContextFactory<NorthwindDb> dbFactory)
    {
        gzipped ??= await BuildAsync(dbFactory);

        context.Response.ContentType = "text/csv; charset=utf-8";
        context.Response.Headers.CacheControl = "public, max-age=3600";
        context.Response.Headers.Vary = "Accept-Encoding";
        if (context.Request.Headers.AcceptEncoding.ToString().Contains("gzip", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.ContentEncoding = "gzip";
            await context.Response.Body.WriteAsync(gzipped, context.RequestAborted);
            return;
        }
        await using var plain = new GZipStream(new MemoryStream(gzipped), CompressionMode.Decompress);
        await plain.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    static async Task<byte[]> BuildAsync(IDbContextFactory<NorthwindDb> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var products = await db.Products.OrderBy(p => p.Id).Select(p => new { p.Name, p.UnitPrice }).ToListAsync();
        var customers = await db.Customers.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
        decimal[] discounts = [0, 0, 0, 0.05m, 0.1m, 0.15m, 0.2m];

        // Written straight into the gzip stream, so the server never holds the whole CSV as a string.
        using var buffer = new MemoryStream();
        await using (var csv = new StreamWriter(new GZipStream(buffer, CompressionLevel.Optimal), new UTF8Encoding(false)))
        {
            csv.NewLine = "\n";
            await csv.WriteLineAsync(Header);
            var random = new Random(11);
            var firstDay = new DateOnly(2025, 1, 1);
            for (var line = 0; line < Lines; line++)
            {
                var order = 30000 + line / 4; // four lines an order
                var product = products[random.Next(products.Count)];
                csv.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{order},{firstDay.AddDays((int)((long)line * 365 / Lines)):yyyy-MM-dd},{customers[order % customers.Count]}," +
                    $"{product.Name.Replace(',', ' ')},{product.UnitPrice:0.00},{random.Next(1, 61)}," +
                    $"{discounts[random.Next(discounts.Length)]:0.00}"));
            }
        }
        return buffer.ToArray();
    }
}
