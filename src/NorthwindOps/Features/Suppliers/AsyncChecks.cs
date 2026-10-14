using System.ComponentModel.DataAnnotations;

namespace NorthwindOps.Features.Suppliers;

// xray:async-form-validation
/// <summary>The supplier code mustn't belong to another supplier: a database lookup, so it can only be checked asynchronously.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueSupplierCodeAttribute() : AsyncValidationAttribute("{0} is already used by another supplier.")
{
    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string { Length: > 0 } code)
        {
            return ValidationResult.Success; // [Required] reports a missing code
        }

        var directory = context.GetRequiredService<SupplierDirectory>();
        return await directory.IsCodeTakenAsync(code, cancellationToken)
            ? new ValidationResult(FormatErrorMessage(context.DisplayName))
            : ValidationResult.Success;
    }

    // There's no honest synchronous answer. EditForm and EditContext.ValidateAsync only ever call IsValidAsync.
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
        => throw new NotSupportedException($"{nameof(UniqueSupplierCodeAttribute)} needs the database: validate with ValidateAsync.");
}

/// <summary>
/// The VAT number must be in the EU VAT register. If the register can't be reached, the check throws:
/// Blazor records that as a faulted validation rather than a validation error.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RegisteredVatNumberAttribute() : AsyncValidationAttribute("The EU VAT register has no record of this {0}.")
{
    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string { Length: > 0 } vatNumber)
        {
            return ValidationResult.Success;
        }

        var register = context.GetRequiredService<VatRegister>();
        return await register.IsRegisteredAsync(vatNumber, cancellationToken)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(context.DisplayName));
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
        => throw new NotSupportedException($"{nameof(RegisteredVatNumberAttribute)} calls a web service: validate with ValidateAsync.");
}
// /xray:async-form-validation
