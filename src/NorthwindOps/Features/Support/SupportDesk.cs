using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Features.Support;

/// <summary>A row in the ticket list.</summary>
public sealed record TicketRow(
    int Id, string Subject, string Company, TicketStatus Status, TicketPriority Priority, DateTime LastActivityOn, int MessageCount, int ReadUpTo)
{
    public int Unread => Math.Max(0, MessageCount - ReadUpTo);
}

/// <summary>A ticket with its whole message thread, oldest first.</summary>
public sealed record TicketView(
    int Id, string Subject, string Company, string? Contact, int? OrderId, TicketStatus Status, TicketPriority Priority, int ReadUpTo, List<TicketMessage> Messages);

/// <summary>Reads and updates support tickets. Each call uses its own short-lived DbContext, so it's safe from a circuit.</summary>
public sealed class SupportDesk(IDbContextFactory<NorthwindDb> dbFactory, TimeProvider clock)
{
    public const string ReplyAuthor = "Northwind Support";
    public const int MaxReplyLength = 4000;

    public async Task<List<TicketRow>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Set<Ticket>()
            .OrderByDescending(t => t.LastActivityOn)
            .Select(t => new TicketRow(t.Id, t.Subject, t.Customer.Company, t.Status, t.Priority, t.LastActivityOn, t.Messages.Count, t.ReadUpTo))
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Set<Ticket>().AnyAsync(t => t.Id == id, ct);
    }

    public async Task<TicketView?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.Set<Ticket>().AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new { t.Id, t.Subject, t.Customer.Company, t.Customer.Contact, t.OrderId, t.Status, t.Priority, t.ReadUpTo })
            .SingleOrDefaultAsync(ct);
        if (ticket is null)
        {
            return null;
        }

        var messages = await db.Set<TicketMessage>().AsNoTracking()
            .Where(m => m.TicketId == id)
            .OrderBy(m => m.Number)
            .ToListAsync(ct);
        return new TicketView(ticket.Id, ticket.Subject, ticket.Company, ticket.Contact, ticket.OrderId, ticket.Status, ticket.Priority, ticket.ReadUpTo, messages);
    }

    // xray:virtualize-scroll-to-item
    /// <summary>
    /// Where a thread opens: the message the link asked for, otherwise the first one nobody has read,
    /// otherwise the latest. Returns a zero-based index, as Virtualize's InitialItemIndex expects.
    /// </summary>
    public static int OpeningIndex(int messageCount, int readUpTo, int? requestedNumber = null)
    {
        if (messageCount <= 0)
        {
            return 0;
        }
        if (requestedNumber is int number && number >= 1 && number <= messageCount)
        {
            return number - 1;
        }
        return Math.Clamp(readUpTo, 0, messageCount - 1);
    }
    // /xray:virtualize-scroll-to-item

    /// <summary>Adds a reply from the support team. Replying means you've read the thread, and the ticket now waits on the customer.</summary>
    public async Task<TicketMessage> ReplyAsync(int ticketId, string body, CancellationToken ct = default)
    {
        body = body.Trim();
        if (body.Length is 0 or > MaxReplyLength)
        {
            throw new ArgumentException($"A reply must be between 1 and {MaxReplyLength} characters.", nameof(body));
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.Set<Ticket>().SingleAsync(t => t.Id == ticketId, ct);
        var number = await db.Set<TicketMessage>().Where(m => m.TicketId == ticketId).MaxAsync(m => (int?)m.Number, ct) ?? 0;
        var reply = new TicketMessage
        {
            TicketId = ticketId,
            Number = number + 1,
            AuthorKind = MessageAuthor.Agent,
            Author = ReplyAuthor,
            SentOn = clock.GetLocalNow().DateTime,
            Body = body,
        };
        db.Add(reply);
        ticket.LastActivityOn = reply.SentOn;
        ticket.ReadUpTo = reply.Number;
        if (ticket.Status == TicketStatus.Open)
        {
            ticket.Status = TicketStatus.Waiting;
        }
        await db.SaveChangesAsync(ct);
        return reply;
    }

    /// <summary>Marks every message in the thread as read. Returns the new read position.</summary>
    public async Task<int> MarkReadAsync(int ticketId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var count = await db.Set<TicketMessage>().CountAsync(m => m.TicketId == ticketId, ct);
        await db.Set<Ticket>().Where(t => t.Id == ticketId).ExecuteUpdateAsync(t => t.SetProperty(x => x.ReadUpTo, count), ct);
        return count;
    }
}
