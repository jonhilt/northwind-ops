using System.Collections.Concurrent;

namespace NorthwindOps.Components.Guide.Demos.CircuitPause;

/// <summary>
/// The auto-pause demo's warehouse system: slow to accept a stock count, and it remembers how
/// many times each count sheet was posted. In memory, for the demo; a real one is elsewhere.
/// </summary>
public static class StockLedger
{
    static readonly ConcurrentDictionary<Guid, (int Posts, DateTimeOffset At)> posts = new();

    /// <summary>Raised when a post lands, from whichever thread made it.</summary>
    public static event Action? Changed;

    public static int Posts(Guid sheet) => posts.TryGetValue(sheet, out var p) ? p.Posts : 0;

    public static async Task PostAsync(Guid sheet)
    {
        await Task.Delay(TimeSpan.FromSeconds(20)); // the warehouse takes its time
        // Forget sheets from over an hour ago, so the list can't grow without limit.
        foreach (var (id, p) in posts)
        {
            if (p.At < DateTimeOffset.UtcNow.AddHours(-1)) posts.TryRemove(id, out _);
        }
        posts.AddOrUpdate(sheet, _ => (1, DateTimeOffset.UtcNow), (_, p) => (p.Posts + 1, DateTimeOffset.UtcNow));
        Changed?.Invoke();
    }
}
