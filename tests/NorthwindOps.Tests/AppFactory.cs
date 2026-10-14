using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NorthwindOps.Tests;

/// <summary>Runs the real app against its own throwaway SQLite database.</summary>
public class AppFactory : WebApplicationFactory<Program>
{
    readonly string databasePath = Path.Combine(Path.GetTempPath(), $"northwind-tests-{Guid.NewGuid():N}.db");

    public string Environment { get; init; } = "Development";

    /// <summary>Per-minute limits for one visitor. A test class makes all its requests as one, so they're lifted unless a test sets them.</summary>
    public int FormPostsPerMinute { get; init; } = 100_000;
    public int CircuitsPerMinute { get; init; } = 100_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.UseSetting("ConnectionStrings:Northwind", $"Data Source={databasePath}");
        builder.UseSetting("RateLimits:FormPostsPerMinute", FormPostsPerMinute.ToString());
        builder.UseSetting("RateLimits:CircuitsPerMinute", CircuitsPerMinute.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
