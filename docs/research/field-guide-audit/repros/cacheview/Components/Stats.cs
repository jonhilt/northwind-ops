namespace CacheViewRepro.Components;
// Counts how many times the slow work ran and how many StockSummary components were created.
public static class Stats
{
    public static int QueriesRun;
    public static int ComponentsCreated;
}
