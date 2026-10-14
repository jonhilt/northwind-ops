using Microsoft.EntityFrameworkCore;

namespace NorthwindOps.Data.Seed;

/// <summary>
/// The public demo shares one database between every visitor, so it's rebuilt on start and then every hour:
/// whatever anyone saves, the data is never more than an hour away from fresh.
/// </summary>
public sealed class DemoData(IDbContextFactory<NorthwindDb> dbFactory, IEnumerable<IDemoDataSeeder> screens, TimeProvider clock)
{
    public static readonly TimeSpan ResetInterval = TimeSpan.FromHours(1);

    readonly SemaphoreSlim gate = new(1, 1);

    public DateTimeOffset LastReset { get; private set; }

    public DateTimeOffset NextReset => LastReset + ResetInterval;

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await NorthwindSeeder.ResetAsync(db, clock.GetLocalNow().Date, screens, ct);
            LastReset = clock.GetUtcNow();
        }
        finally
        {
            gate.Release();
        }
    }
}

public sealed class HourlyDemoDataReset(DemoData demoData, ILogger<HourlyDemoDataReset> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(DemoData.ResetInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await demoData.ResetAsync(stoppingToken);
                logger.LogInformation("Demo data reset");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Demo data reset failed; trying again next hour");
            }
        }
    }
}
