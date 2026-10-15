using System.ComponentModel.DataAnnotations;

namespace NorthwindOps.Components.Guide.Demos.Labels;

/// <summary>What the "New supplier" form posts, on both sides of the demo.</summary>
public sealed class NewSupplierForm
{
    // xray:label-and-input-ids
    // Not new: [Display] already named the validation messages in .NET 10.
    // New in .NET 11: <Label For> reads the same name, so the label can't drift from them.
    [Required, StringLength(40), Display(Name = "Company name")]
    public string? Company { get; set; }

    [Required, EmailAddress, StringLength(60), Display(Name = "Contact email")]
    public string? Email { get; set; }
    // /xray:label-and-input-ids
}
