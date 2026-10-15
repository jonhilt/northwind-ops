namespace NorthwindOps.Components.Guide.Demos.AnchorMode;

/// <summary>A message in the support inbox. Both feeds start with the same 300.</summary>
public record InboxMessage(int N, string From, string Text, DateTime At)
{
    static readonly string[] Customers = ["Ana Trujillo", "Thomas Hardy", "Hanna Moos"];
    const string Damaged = "The pallet arrived damaged and we need a replacement. ";

    public static List<InboxMessage> Earlier() => [.. Enumerable.Range(0, 300).Select(Make)];

    // Order numbers count up. Every third message runs to several lines,
    // so the rows have mixed heights, as in a real inbox.
    public static InboxMessage Make(int n) => new(n, Customers[n % 3],
        n % 3 == 0
            ? $"Order {10900 + n}: " + string.Concat(Enumerable.Repeat(Damaged, 1 + n % 4))
            : $"Has order {10900 + n} shipped?",
        DateTime.Now);
}
