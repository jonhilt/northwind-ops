using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace NorthwindOps.Data.Seed;

/// <summary>
/// Fills in the onboarding details the Suppliers screen needs (codes, emails, VAT numbers, addresses)
/// for the Northwind suppliers, so the "supplier code is already taken" check has real codes to clash with.
/// </summary>
public sealed class SupplierSeeder : IDemoDataSeeder
{
    static readonly Dictionary<string, string> VatPrefixes = new()
    {
        ["UK"] = "GB", ["Germany"] = "DE", ["France"] = "FR", ["Italy"] = "IT", ["Spain"] = "ES", ["Sweden"] = "SE",
        ["Denmark"] = "DK", ["Netherlands"] = "NL", ["Finland"] = "FI", ["Norway"] = "NO",
    };

    public async Task SeedAsync(NorthwindDb db, DateTime today, CancellationToken ct)
    {
        var random = new Random(2611);
        var codes = new HashSet<string>();

        foreach (var supplier in await db.Set<Supplier>().OrderBy(s => s.Id).ToListAsync(ct))
        {
            var letters = Letters(supplier.Company);
            var number = 1;
            while (!codes.Add($"{letters}-{number:00}"))
            {
                number++;
            }

            var domain = Letters(supplier.Company, 40).ToLowerInvariant();
            supplier.Code = $"{letters}-{number:00}";
            supplier.Website = $"https://www.{domain}.example";
            supplier.Email = supplier.Contact is { } contact
                ? $"{contact.Split(' ')[0].ToLowerInvariant()}@{domain}.example"
                : $"orders@{domain}.example";
            supplier.Street = $"{random.Next(1, 220)} {Streets[random.Next(Streets.Length)]}";
            supplier.PostalCode = random.Next(10000, 99999).ToString(CultureInfo.InvariantCulture);
            supplier.VatNumber = supplier.Country is { } country && VatPrefixes.TryGetValue(country, out var prefix)
                ? $"{prefix}{random.Next(100_000_000, 999_999_999)}"
                : null;
        }

        db.Set<SupplierContact>().AddRange(
            new SupplierContact { SupplierId = 1, Name = "Ruth Okafor", Role = "Accounts", Email = "accounts@exoticliquids.example" },
            new SupplierContact { SupplierId = 1, Name = "Tom Hale", Role = "Dispatch", Email = "dispatch@exoticliquids.example" },
            new SupplierContact { SupplierId = 12, Name = "Jana Vogel", Role = "Quality", Email = "jana.vogel@plutzer.example" });
    }

    static readonly string[] Streets = ["High Street", "Market Road", "Harbour Lane", "Mill Way", "Station Road", "Church Street"];

    // "Heli Süßwaren GmbH" -> "HELI": letters only, accents dropped, upper case.
    static string Letters(string company, int length = 4)
    {
        var letters = company.Normalize(NormalizationForm.FormD)
            .Where(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
            .Take(length)
            .ToArray();
        return new string(letters).ToUpperInvariant().PadRight(Math.Min(length, 4), 'X');
    }
}
