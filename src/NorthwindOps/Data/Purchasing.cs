using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NorthwindOps.Data;

public enum PurchaseOrderStatus
{
    Sent,
    Confirmed,
    Received,
    Cancelled,
}

/// <summary>An order we place with a supplier to restock the warehouse.</summary>
public class PurchaseOrder
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;
    public DateTime RaisedOn { get; set; }
    public DateTime DeliverBy { get; set; }
    public PurchaseOrderStatus Status { get; set; }

    // Stored rather than computed so the list can show and sort it without loading the lines.
    public decimal Total { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = [];

    public string Number => $"PO-{Id}";
}

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> po)
    {
        po.Property(p => p.Status).HasConversion<string>();
        po.HasIndex(p => p.RaisedOn);
        po.Ignore(p => p.Number);
        po.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId);
    }
}
