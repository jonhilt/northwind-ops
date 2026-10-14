using System.ComponentModel.DataAnnotations;

namespace NorthwindOps.Components.Guide.Demos.Labels;

/// <summary>What the "New supplier" form posts.</summary>
public sealed class NewSupplierForm
{
    // xray:label-and-input-ids
    // Each field's name, written once: its label and its validation messages both use it.
    [Required, StringLength(40), Display(Name = "Company name")]
    public string? Company { get; set; }

    [Required, EmailAddress, StringLength(60), Display(Name = "Contact email")]
    public string? Email { get; set; }
    // /xray:label-and-input-ids
}
