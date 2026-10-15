namespace Repro.Components;

public record Message(int Number, string Author, string Body);

public static class Thread
{
    // 500 messages: mostly one-line log entries, every 5th a long email (12 lines).
    public static readonly List<Message> Messages = Enumerable.Range(1, 500)
        .Select(n => new Message(n, n % 2 == 0 ? "Ana" : "Bot",
            n % 5 == 0 ? string.Join("\n", Enumerable.Range(1, 12).Select(i => $"Long email line {i}"))
                       : $"log entry {n}"))
        .ToList();
}
