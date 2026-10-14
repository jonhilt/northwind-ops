using System.Collections.Concurrent;

namespace NorthwindOps.Components.Guide.Demos.CircuitPause;

/// <summary>
/// One per server, for the circuit pause demos: how many circuits are open, and how many tabs are
/// paused. A paused tab holds no circuit here; its state waits in the browser until it resumes.
/// </summary>
public sealed class CircuitBoard(TimeProvider clock)
{
    const int MaxPaused = 1_000;
    readonly ConcurrentDictionary<string, DateTimeOffset> paused = new();
    int open;

    public int Open => Volatile.Read(ref open);
    public int Paused => paused.Count;

    /// <summary>Raised on any change, from whichever thread made it.</summary>
    public event Action? Changed;

    public void Opened() { Interlocked.Increment(ref open); Changed?.Invoke(); }

    public void Closed() { Interlocked.Decrement(ref open); Changed?.Invoke(); }

    public void Pausing(string circuitId)
    {
        // Forget tabs that never came back, so the list can't grow without limit.
        var cutoff = clock.GetUtcNow().AddHours(-1);
        foreach (var (id, at) in paused)
        {
            if (at < cutoff || paused.Count >= MaxPaused) paused.TryRemove(id, out _);
        }
        paused[circuitId] = clock.GetUtcNow();
        Changed?.Invoke();
    }

    /// <summary>How long the tab whose circuit was <paramref name="circuitId"/> stayed paused.</summary>
    public TimeSpan? Resumed(string circuitId)
    {
        if (!paused.TryRemove(circuitId, out var at)) return null;
        Changed?.Invoke();
        return clock.GetUtcNow() - at;
    }
}
