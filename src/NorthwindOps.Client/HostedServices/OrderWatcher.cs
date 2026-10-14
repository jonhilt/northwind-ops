using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;

namespace NorthwindOps.Client.HostedServices;

public sealed record OrderUpdate(long Id, string Order, string Detail);

/// <summary>Asks the server for order updates every 3 seconds.</summary>
// xray:wasm-hosted-services
public sealed class OrderWatcher(HttpClient http) : BackgroundService
// /xray:wasm-hosted-services
{
    long lastSeen;
    bool started;

    public int Count { get; private set; }
    public OrderUpdate? Latest { get; private set; }
    public DateTime? CheckedAt { get; private set; }
    public event Action? Changed;

    // xray:wasm-hosted-services
    // The host starts this as WebAssembly starts. It runs until the page unloads.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (Changed is null) continue; // no bell on this page: skip the call
            // Server unreachable for a moment? Try again on the next tick.
            try { await CheckAsync(stoppingToken); }
            catch (Exception) when (!stoppingToken.IsCancellationRequested) { }
        }
    }
    // /xray:wasm-hosted-services

    async Task CheckAsync(CancellationToken cancel)
    {
        var updates = await http.GetFromJsonAsync<OrderUpdate[]>(
            $"demo/hosted-services/api/updates?after={lastSeen}", cancel) ?? [];
        if (started) Count += updates.Length; // the first check just finds the newest
        if (updates.Length > 0) (Latest, lastSeen) = (updates[^1], updates[^1].Id);
        (started, CheckedAt) = (true, DateTime.Now);
        Changed?.Invoke();
    }
}
