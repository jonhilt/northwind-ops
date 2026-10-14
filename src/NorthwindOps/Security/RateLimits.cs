using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace NorthwindOps.Security;

/// <summary>How often one visitor may do the things that cost the server memory or work, per minute.</summary>
public sealed class RateLimits
{
    /// <summary>Form posts: saving an order, a supplier, a purchase order step.</summary>
    public int FormPostsPerMinute { get; set; } = 60;

    /// <summary>New circuits. Generous, because a room at a talk can share one IP address.</summary>
    public int CircuitsPerMinute { get; set; } = 120;

    /// <summary>The Web Worker demo's CSV, which is large.</summary>
    public int DownloadsPerMinute { get; set; } = 10;

    public static void Add(WebApplicationBuilder builder)
    {
        builder.Services.Configure<RateLimits>(builder.Configuration.GetSection("RateLimits"));
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // A request re-run to render the not-found or error page was counted when it first came in.
                if (context.Features.Get<IStatusCodeReExecuteFeature>() is not null || context.Features.Get<IExceptionHandlerFeature>() is not null)
                {
                    return RateLimitPartition.GetNoLimiter("");
                }
                var limits = context.RequestServices.GetRequiredService<IOptions<RateLimits>>().Value;
                var (kind, perMinute) = Classify(context.Request, limits);
                return kind is null
                    ? RateLimitPartition.GetNoLimiter("")
                    : RateLimitPartition.GetFixedWindowLimiter($"{kind}:{Visitor(context)}", _ => new()
                    {
                        PermitLimit = perMinute,
                        Window = TimeSpan.FromMinutes(1),
                    });
            });
        });
    }

    static (string? Kind, int PerMinute) Classify(HttpRequest request, RateLimits limits) => request switch
    {
        _ when request.Path.StartsWithSegments("/_blazor/negotiate") => ("circuit", limits.CircuitsPerMinute),
        _ when request.Path.StartsWithSegments("/_blazor") => (null, 0), // a circuit's own traffic, once it's started
        _ when HttpMethods.IsPost(request.Method) => ("post", limits.FormPostsPerMinute),
        _ when request.Path.Value?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true => ("download", limits.DownloadsPerMinute),
        _ => (null, 0),
    };

    /// <summary>
    /// Cloudflare sets CF-Connecting-IP to the real client, overwriting any the client sent. The app is reachable only
    /// through its Cloudflare Tunnel, so the header can be trusted there; anywhere else it falls back to the connection.
    /// </summary>
    static string Visitor(HttpContext context) =>
        context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
