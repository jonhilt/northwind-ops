using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NorthwindOps.Data;

public enum SupplierStatus
{
    Active,
    Draft,
}

/// <summary>Someone else to talk to at a supplier: accounts, dispatch, quality...</summary>
public class SupplierContact
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public required string Name { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> supplier)
    {
        // Codes are unique. Drafts can be saved without one, and SQLite lets any number of rows share NULL.
        supplier.HasIndex(s => s.Code).IsUnique();
        supplier.Property(s => s.Status).HasConversion<string>();
    }
}

public sealed class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> contact)
    {
        contact.HasOne<Supplier>().WithMany().HasForeignKey(c => c.SupplierId).OnDelete(DeleteBehavior.Cascade);
    }
}
