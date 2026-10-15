using Microsoft.Extensions.Hosting;

namespace WasmHostedRepro;

/// <summary>A BackgroundService that ticks once a second. Two subclasses so DI can tell them apart.</summary>
public abstract class Watcher : BackgroundService
{
    public bool Started { get; private set; }
    public int Ticks { get; private set; }
    public event Action? Changed;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Started = true;
        Console.WriteLine($"{GetType().Name}.ExecuteAsync started");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken)) { Ticks++; Changed?.Invoke(); }
    }
}

/// <summary>Registered with AddHostedService, exactly as the .NET 11 guide does it.</summary>
public sealed class HostedWatcher : Watcher;

/// <summary>.NET 10 workaround: singleton started by hand in Program.cs.</summary>
public sealed class ManualWatcher : Watcher;
