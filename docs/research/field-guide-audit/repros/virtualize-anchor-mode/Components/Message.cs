namespace Repro.Components;

public record Message(int N, string Text)
{
    // Order numbers count up; every 3rd message is long enough to wrap onto several lines.
    public static Message Make(int n, bool mixed) => new(n,
        mixed && n % 3 == 0
            ? $"Order {10900 + n}: " + string.Concat(Enumerable.Repeat("the pallet arrived damaged and I need a replacement. ", 1 + n % 4))
            : $"Order {10900 + n}: has it shipped?");
}
