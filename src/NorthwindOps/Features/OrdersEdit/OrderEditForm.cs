using System.ComponentModel.DataAnnotations;
using NorthwindOps.Data;

namespace NorthwindOps.Features.OrdersEdit;

/// <summary>The fields a warehouse or account manager can change on an order.</summary>
public sealed class OrderEditForm
{
    public const int MaxNotesLength = 500;

    [EnumDataType(typeof(OrderStatus))] // a posted "42" binds, so reject numbers that aren't a status
    public OrderStatus Status { get; set; }

    public DateOnly? ShippedOn { get; set; }

    [StringLength(MaxNotesLength, ErrorMessage = "Keep the note under 500 characters.")]
    public string? Notes { get; set; }

    public static OrderEditForm From(Order order) => new()
    {
        Status = order.Status,
        ShippedOn = order.ShippedOn is { } shipped ? DateOnly.FromDateTime(shipped) : null,
        Notes = order.Notes,
    };

    public static bool HasShipped(OrderStatus status) => status is OrderStatus.Shipped or OrderStatus.Delivered;

    /// <summary>
    /// Rules that depend on the order itself and on today's date, so they can't be attributes.
    /// Each result names the field it belongs to.
    /// </summary>
    public IEnumerable<ValidationResult> Check(DateTime placedOn, DateOnly today)
    {
        string[] shippedOn = [nameof(ShippedOn)];

        if (HasShipped(Status) && ShippedOn is null)
        {
            yield return new($"A {Status.ToString().ToLowerInvariant()} order needs a shipped date.", shippedOn);
        }
        else if (!HasShipped(Status) && ShippedOn is not null)
        {
            yield return new("Only shipped or delivered orders have a shipped date. Clear the date or change the status.", shippedOn);
        }

        if (ShippedOn is { } date)
        {
            if (date < DateOnly.FromDateTime(placedOn))
            {
                yield return new($"An order can't ship before it was placed ({placedOn:d MMM yyyy}).", shippedOn);
            }
            else if (date > today)
            {
                yield return new("The shipped date can't be in the future.", shippedOn);
            }
        }
    }

    /// <summary>Copies the form onto the order. A shipped date that hasn't changed keeps its original time of day.</summary>
    public void ApplyTo(Order order)
    {
        order.Status = Status;
        order.ShippedOn = ShippedOn switch
        {
            null => null,
            { } date when order.ShippedOn is { } existing && DateOnly.FromDateTime(existing) == date => existing,
            { } date => date.ToDateTime(TimeOnly.MinValue),
        };
        order.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
    }
}
