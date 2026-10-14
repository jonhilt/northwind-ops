using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Features.Suppliers;

/// <summary>Looks up and adds suppliers for the Suppliers screen.</summary>
public sealed class SupplierDirectory(IDbContextFactory<NorthwindDb> dbFactory, TimeProvider clock)
{
    /// <summary>How long a code lookup takes. Deliberately slow, like a busy ERP, so the "checking" state is visible.</summary>
    public static readonly TimeSpan LookupTime = TimeSpan.FromSeconds(1.5);

    // xray:async-form-validation
    public async Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken)
    {
        await Task.Delay(LookupTime, clock, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<Supplier>().AnyAsync(s => s.Code == code, cancellationToken);
    }
    // /xray:async-form-validation

    /// <summary>Adds the supplier and returns its id. Drafts keep their code only if it's free, because codes are unique.</summary>
    public async Task<SavedSupplier> AddAsync(SupplierForm form, SupplierStatus status, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var code = form.Code?.Trim();
        var codeDropped = false;
        if (status == SupplierStatus.Draft && !string.IsNullOrEmpty(code) && await db.Set<Supplier>().AnyAsync(s => s.Code == code, cancellationToken))
        {
            code = null;
            codeDropped = true;
        }

        var supplier = new Supplier
        {
            Company = form.Company!.Trim(),
            Code = string.IsNullOrEmpty(code) ? null : code,
            Contact = form.Contact,
            Email = form.Email,
            Phone = form.Phone,
            Website = form.Website,
            VatNumber = form.VatNumber,
            Street = form.Address.Street,
            City = form.Address.City,
            PostalCode = form.Address.PostalCode,
            Country = form.Address.Country,
            Status = status,
        };
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(cancellationToken);

        db.Set<SupplierContact>().AddRange(form.Contacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new SupplierContact { SupplierId = supplier.Id, Name = c.Name!, Role = c.Role, Email = c.Email }));
        await db.SaveChangesAsync(cancellationToken);

        return new SavedSupplier(supplier.Id, codeDropped);
    }

    public async Task<List<SupplierRow>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Set<Supplier>()
            .Select(s => new SupplierRow(s.Id, s.Code, s.Company, s.Contact, s.City, s.Country, s.Phone, s.Status))
            .ToListAsync(cancellationToken);

        // Sorted here rather than in SQL, so "Forêts d'érables" sorts the way a person expects.
        return [.. rows.OrderBy(s => s.Company, StringComparer.CurrentCulture)];
    }
}

public sealed record SavedSupplier(int Id, bool CodeDropped);

public sealed record SupplierRow(int Id, string? Code, string Company, string? Contact, string? City, string? Country, string? Phone, SupplierStatus Status);
