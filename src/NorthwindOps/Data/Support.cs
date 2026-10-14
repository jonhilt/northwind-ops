using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NorthwindOps.Data;

public enum TicketStatus
{
    Open,
    Waiting, // on the customer
    Resolved,
}

public enum TicketPriority
{
    Low,
    Normal,
    High,
    Urgent,
}

public enum MessageAuthor
{
    Customer,
    Agent,
    System,
}

/// <summary>A customer support ticket and its message thread.</summary>
public class Ticket
{
    public int Id { get; set; }
    public required string Subject { get; set; }
    public required string CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public int? OrderId { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTime OpenedOn { get; set; }
    public DateTime LastActivityOn { get; set; }

    /// <summary>How many messages the support team has read, from the start of the thread.</summary>
    public int ReadUpTo { get; set; }

    public List<TicketMessage> Messages { get; set; } = [];
}

public class TicketMessage
{
    public int Id { get; set; }
    public int TicketId { get; set; }

    /// <summary>1-based position in the thread, as shown to agents ("message #187").</summary>
    public int Number { get; set; }

    public MessageAuthor AuthorKind { get; set; }
    public required string Author { get; set; }
    public DateTime SentOn { get; set; }

    /// <summary>Plain text. Paragraphs are separated by a blank line; lines starting "- " are bullets.</summary>
    public required string Body { get; set; }
}

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> ticket)
    {
        ticket.Property(t => t.Status).HasConversion<string>();
        ticket.Property(t => t.Priority).HasConversion<string>();
        ticket.HasIndex(t => t.LastActivityOn);
        ticket.HasMany(t => t.Messages).WithOne().HasForeignKey(m => m.TicketId);
    }
}

public sealed class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> message)
    {
        message.Property(m => m.AuthorKind).HasConversion<string>();
        message.HasIndex(m => new { m.TicketId, m.Number }).IsUnique();
    }
}
