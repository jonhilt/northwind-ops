using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;

namespace NorthwindOps.Client.HostedServices;

public sealed record OrderUpdate(long Id, string Order, string Detail);

/// <summary>Asks the server for order updates every 3 seconds.</summary>
// An ordinary BackgroundService, the same class a server app would use.
public sealed class OrderWatcher(HttpClient http) : BackgroundService
{
    long lastSeen;
    int checks;

    public int Count { get; private set; }
    public OrderUpdate? Latest { get; private set; }
    public event Action? Changed;

    // xray:wasm-hosted-services
    // New in .NET 11: the WebAssembly host calls this as it starts. It runs until the tab closes.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    // /xray:wasm-hosted-services
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Other demos share this client: wait for a bell. After that, check on every tick.
            if (Changed is null && checks == 0) continue;
            // Server unreachable for a moment? Try again on the next tick.
            try { await CheckAsync(stoppingToken); }
            catch (Exception) when (!stoppingToken.IsCancellationRequested) { }
        }
    }

    async Task CheckAsync(CancellationToken cancel)
    {
        var updates = await http.GetFromJsonAsync<OrderUpdate[]>(
            $"demo/hosted-services/api/updates?after={lastSeen}", cancel) ?? [];
        if (updates.Length > 0) lastSeen = updates[^1].Id;
        // The first check only finds where the feed is up to. Updates count after that.
        if (checks++ > 0 && updates.Length > 0)
            (Count, Latest) = (Count + updates.Length, updates[^1]);
        Changed?.Invoke();
    }
}
