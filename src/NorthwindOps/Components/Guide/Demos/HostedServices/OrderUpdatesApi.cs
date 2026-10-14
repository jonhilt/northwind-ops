using NorthwindOps.Client.HostedServices;
using NorthwindOps.Features.Dashboard;

namespace NorthwindOps.Components.Guide.Demos.HostedServices;

/// <summary>The hosted-services demo's API: order updates from the warehouse's live feed.</summary>
public static class OrderUpdatesApi
{
    public static void MapOrderUpdatesApi(this IEndpointRouteBuilder app) =>
        // Orders placed, packed or shipped since the update the browser saw last. At most ten.
        app.MapGet("demo/hosted-services/api/updates", (long after, OrderFeed feed) =>
            feed.Read(feed.Count - 50, 50)
                .Where(e => e.Kind != OrderEventKind.StockAlert && e.Id > after)
                .TakeLast(10)
                .Select(e => new OrderUpdate(e.Id, e.Subject, e.Detail)));
}
