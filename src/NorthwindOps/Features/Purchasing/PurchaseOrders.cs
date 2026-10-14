using NorthwindOps.Data;

namespace NorthwindOps.Features.Purchasing;

/// <summary>The steps of the new purchase order wizard. Each one is a URL segment under /purchasing/new/.</summary>
public enum WizardStep
{
    Supplier = 1,
    Lines = 2,
    Review = 3,
}

/// <summary>
/// The session keys that hold a purchase order draft. The wizard and the draft banner on the list both bind them
/// with [SupplyParameterFromSession], so they share the names.
/// </summary>
public static class DraftKeys
{
    public const string Step = "po-draft-step";
    public const string Supplier = "po-draft-supplier";
    public const string Lines = "po-draft-lines";
    public const string Started = "po-draft-started";
}

/// <summary>Purchasing rules shared by the wizard, the seeder and the tests.</summary>
public static class PurchaseOrders
{
    public const int MaxQuantity = 999;

    public static string Slug(this WizardStep step) => step.ToString().ToLowerInvariant();

    /// <summary>
    /// How much to reorder when stock has fallen below the reorder level: enough to get back to twice the level,
    /// in whole fives, because suppliers ship by the case.
    /// </summary>
    public static int SuggestedQuantity(Product product)
    {
        if (product.Discontinued || product.ReorderLevel == 0 || product.UnitsInStock >= product.ReorderLevel)
        {
            return 0;
        }
        var shortfall = product.ReorderLevel * 2 - product.UnitsInStock;
        return (int)Math.Ceiling(shortfall / 5.0) * 5;
    }

    /// <summary>
    /// Builds the lines of a new purchase order from the draft. Lines for products that aren't the supplier's,
    /// are discontinued, or have no quantity are dropped.
    /// </summary>
    public static List<PurchaseOrderLine> LinesFor(IReadOnlyDictionary<string, int>? draft, IEnumerable<Product> supplierProducts)
    {
        if (draft is null)
        {
            return [];
        }
        return supplierProducts
            .Where(p => !p.Discontinued && draft.GetValueOrDefault(p.Id.ToString()) > 0)
            .OrderBy(p => p.Name)
            .Select(p => new PurchaseOrderLine
            {
                ProductId = p.Id,
                Product = p,
                Quantity = Math.Min(draft[p.Id.ToString()], MaxQuantity),
                UnitPrice = p.UnitPrice,
            })
            .ToList();
    }

    public static decimal TotalOf(IEnumerable<PurchaseOrderLine> lines) => Math.Round(lines.Sum(l => l.UnitPrice * l.Quantity), 2);
}
