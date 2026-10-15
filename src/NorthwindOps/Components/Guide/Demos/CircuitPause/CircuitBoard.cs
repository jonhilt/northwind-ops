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
    readonly ConcurrentDictionary<string, PauseRequest> requests = new();
    int open;

    public int Open => Volatile.Read(ref open);
    public int Paused => paused.Count;

    /// <summary>Raised on any change, from whichever thread made it.</summary>
    public event Action? Changed;

    public void Opened() { Interlocked.Increment(ref open); Changed?.Invoke(); }

    public void Closed() { Interlocked.Decrement(ref open); Changed?.Invoke(); }

    public void Pausing(string circuitId)
    {
        Forget(paused, at => at);
        paused[circuitId] = clock.GetUtcNow();
        Changed?.Invoke();
    }

    /// <summary>Server code asked this circuit's tab to pause: remember when, until it answers.</summary>
    public void Asked(string circuitId)
    {
        Forget(requests, r => r.At);
        requests[circuitId] = new(clock.GetUtcNow(), null, null);
    }

    public void Answered(string circuitId, string answer)
    {
        if (requests.TryGetValue(circuitId, out var asked))
        {
            requests[circuitId] = asked with { Answer = answer, After = clock.GetUtcNow() - asked.At };
        }
    }

    /// <summary>What became of the server's request to pause this circuit, if it made one.</summary>
    public PauseRequest? Request(string circuitId) => requests.GetValueOrDefault(circuitId);

    /// <summary>How long the tab whose circuit was <paramref name="circuitId"/> stayed paused.</summary>
    public TimeSpan? Resumed(string circuitId)
    {
        if (!paused.TryRemove(circuitId, out var at)) return null;
        Changed?.Invoke();
        return clock.GetUtcNow() - at;
    }

    // Forget tabs that never came back, so the lists can't grow without limit.
    void Forget<T>(ConcurrentDictionary<string, T> list, Func<T, DateTimeOffset> at)
    {
        var cutoff = clock.GetUtcNow().AddHours(-1);
        foreach (var (id, value) in list)
        {
            if (at(value) < cutoff || list.Count >= MaxPaused) list.TryRemove(id, out _);
        }
    }
}

/// <summary>When server code asked a tab to pause, and what it heard back (null: nothing yet).</summary>
public sealed record PauseRequest(DateTimeOffset At, string? Answer, TimeSpan? After);
