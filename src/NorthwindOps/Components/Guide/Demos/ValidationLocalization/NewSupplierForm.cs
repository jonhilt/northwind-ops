using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace NorthwindOps.Components.Guide.Demos.ValidationLocalization;

/// <summary>What the "New supplier" form posts.</summary>
// xray:validation-localization
// AddValidation() validates [ValidatableType] models, and translates their messages.
[ValidatableType]
// /xray:validation-localization
public sealed class NewSupplierForm
{
    // xray:validation-localization
    // No message settings on any attribute. Each rule's message is looked up by its name.
    [Required, StringLength(40), Display(Name = "Company")]
    public string? Company { get; set; }

    [Required, EmailAddress, StringLength(60), Display(Name = "Email")]
    public string? Email { get; set; }
    // /xray:validation-localization
}
