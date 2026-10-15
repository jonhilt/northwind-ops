using NorthwindOps.Worker;

namespace NorthwindOps.Client.WebWorker;

/// <summary>One press of a Total button: how long it took, and how often the clock ticked meanwhile.</summary>
public record ReportRun(string How, TimeSpan Took, int Ticked, OrderLineTotals Totals)
{
    // The clock ticks every 100 ms, so this is how many ticks a free UI thread would have drawn.
    public int Expected => (int)Math.Round(Took.TotalMilliseconds / 100);
}
