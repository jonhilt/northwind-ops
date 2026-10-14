using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NorthwindOps.Data;

namespace NorthwindOps.Components.Guide.Demos.AsyncValidation;

// xray:async-form-validation
/// <summary>Is the code free? Only the database knows.</summary>
public sealed class UniqueSupplierCodeAttribute()
    : AsyncValidationAttribute("{0} is already used by another supplier.")
{
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string { Length: > 0 } code) return ValidationResult.Success;

        // Slow on purpose, like a busy ERP. A newer edit cancels this check.
        await Task.Delay(1500, cancellationToken);

        var dbFactory = context.GetRequiredService<IDbContextFactory<NorthwindDb>>();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var taken = await db.Set<Supplier>()
            .AnyAsync(s => s.Code == code, cancellationToken);

        // Name the field, or a check run by Save puts its message on no field at all.
        return taken
            ? new ValidationResult(FormatErrorMessage(context.DisplayName),
                [context.MemberName!])
            : ValidationResult.Success;
    }

    // There's no honest synchronous answer. EditForm only calls IsValidAsync.
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
        => throw new NotSupportedException("Validate with EditContext.ValidateAsync.");
}
// /xray:async-form-validation
