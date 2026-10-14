namespace NorthwindOps.Data;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class Supplier
{
    public int Id { get; set; }
    public required string Company { get; set; }
    public string? Contact { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }

    // Suppliers screen: onboarding details (see Data/Suppliers.cs for SupplierContact and the configuration).
    public string? Code { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? VatNumber { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public SupplierStatus Status { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = default!;
    public string? QuantityPerUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public int UnitsInStock { get; set; }
    public int ReorderLevel { get; set; }
    public bool Discontinued { get; set; }
}

public class Customer
{
    public required string Id { get; set; }
    public required string Company { get; set; }
    public string? Contact { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public enum OrderStatus
{
    Pending,
    Packing,
    Shipped,
    Delivered,
    Cancelled,
}

public class Order
{
    public int Id { get; set; }
    public required string CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTime PlacedOn { get; set; }
    public DateTime? ShippedOn { get; set; }
    public OrderStatus Status { get; set; }

    // Stored rather than computed so the grid can sort by it in SQL.
    public decimal Total { get; set; }

    // An internal note for the warehouse and account managers; customers never see it.
    public string? Notes { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
}
