using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Components.Guide.Demos.DisplayNames;

/// <summary>The rows both grids list: the first seeded suppliers, not ones visitors added.</summary>
public static class SupplierRows
{
    public static async Task<IQueryable<Supplier>> LoadAsync(IDbContextFactory<NorthwindDb> dbs)
    {
        await using var db = await dbs.CreateDbContextAsync();
        return (await db.Set<Supplier>().Where(s => s.Code != null)
            .OrderBy(s => s.Id).Take(8).ToListAsync()).AsQueryable();
    }
}
