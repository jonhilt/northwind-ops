using System.ComponentModel.DataAnnotations;

namespace NorthwindOps.Components.Guide.Demos.AsyncValidation;

/// <summary>What the .NET 11 "New supplier" form edits: the async rule sits with the others.</summary>
public sealed class NewSupplierForm
{
    [Required, StringLength(40), Display(Name = "Company")]
    public string? Company { get; set; }

    [Required, Display(Name = "Supplier code")]
    [RegularExpression("^[A-Z]{4}-[0-9]{2}$",
        ErrorMessage = "Use 4 capital letters, a dash and 2 digits.")]
    // xray:async-form-validation
    // An async rule sits with the others. It only runs once they pass.
    [UniqueSupplierCode]
    // /xray:async-form-validation
    public string? Code { get; set; }
}

/// <summary>The same fields for the .NET 10 form, whose code check lives in the page instead.</summary>
public sealed class NewSupplierFormNet10
{
    [Required, StringLength(40), Display(Name = "Company")]
    public string? Company { get; set; }

    [Required, Display(Name = "Supplier code")]
    [RegularExpression("^[A-Z]{4}-[0-9]{2}$",
        ErrorMessage = "Use 4 capital letters, a dash and 2 digits.")]
    public string? Code { get; set; }
}
