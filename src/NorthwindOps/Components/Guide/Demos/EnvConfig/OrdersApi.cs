using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Components.Guide.Demos.EnvConfig;

/// <summary>The server side of the environment-variables demo: the orders API the browser calls.</summary>
public static class OrdersApi
{
    /// <summary>Where this server maps its orders API.</summary>
    public const string Url = "demo/env-config/api/orders";

    public static void MapOrdersApi(this IEndpointRouteBuilder app) =>
        app.MapGet(Url, async (int take, IDbContextFactory<NorthwindDb> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await db.Orders.OrderByDescending(o => o.Id).Take(Math.Clamp(take, 1, 20))
                .Select(o => new { o.Id, Customer = o.Customer.Company, o.Total })
                .ToListAsync();
        });
}
