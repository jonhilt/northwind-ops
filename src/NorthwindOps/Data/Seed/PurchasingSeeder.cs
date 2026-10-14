using Microsoft.EntityFrameworkCore;
using NorthwindOps.Features.Purchasing;

namespace NorthwindOps.Data.Seed;

/// <summary>A few weeks of purchase orders, newest still waiting on the supplier, oldest received.</summary>
public sealed class PurchasingSeeder : IDemoDataSeeder
{
    public const int FirstPurchaseOrderId = 2401;

    // (days ago, supplier) for each order, oldest first.
    static readonly (int DaysAgo, int SupplierId)[] Schedule =
    [
        (26, 7), (23, 12), (19, 1), (16, 14), (12, 8), (9, 17), (6, 2), (4, 22), (2, 20), (1, 14),
    ];

    public async Task SeedAsync(NorthwindDb db, DateTime today, CancellationToken ct)
    {
        var random = new Random(2401);
        var products = await db.Products.Where(p => !p.Discontinued).ToListAsync(ct);
        var id = FirstPurchaseOrderId;

        foreach (var (daysAgo, supplierId) in Schedule)
        {
            var raised = today.AddDays(-daysAgo).AddHours(random.Next(8, 17)).AddMinutes(random.Next(60));
            var draft = products
                .Where(p => p.SupplierId == supplierId)
                .OrderBy(_ => random.Next())
                .Take(random.Next(1, 4))
                .ToDictionary(p => p.Id.ToString(), p => Math.Max(PurchaseOrders.SuggestedQuantity(p), random.Next(2, 9) * 5));
            var lines = PurchaseOrders.LinesFor(draft, products.Where(p => p.SupplierId == supplierId));

            var status = daysAgo switch
            {
                >= 14 => PurchaseOrderStatus.Received,
                12 => PurchaseOrderStatus.Cancelled,
                >= 5 => PurchaseOrderStatus.Confirmed,
                _ => PurchaseOrderStatus.Sent,
            };
            var deliverBy = raised.Date.AddDays(random.Next(7, 15));
            if (status is (PurchaseOrderStatus.Sent or PurchaseOrderStatus.Confirmed) && deliverBy <= today)
            {
                deliverBy = today.AddDays(random.Next(2, 6)); // still on its way, so not overdue
            }

            db.Set<PurchaseOrder>().Add(new PurchaseOrder
            {
                Id = id++,
                SupplierId = supplierId,
                RaisedOn = raised,
                DeliverBy = deliverBy,
                Status = status,
                Lines = lines,
                Total = PurchaseOrders.TotalOf(lines),
            });
        }
    }
}
