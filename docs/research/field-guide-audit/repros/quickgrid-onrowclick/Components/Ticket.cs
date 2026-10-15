namespace Repro.Components;

public record TicketRow(int Id, string Subject, string Company);

public static class Tickets
{
    public static readonly IQueryable<TicketRow> All = new[]
    {
        new TicketRow(101, "Invoice total looks wrong", "Alfreds Futterkiste"),
        new TicketRow(102, "Delivery arrived damaged", "Ernst Handel"),
        new TicketRow(103, "Can't log in to portal", "Island Trading"),
    }.AsQueryable();
}
