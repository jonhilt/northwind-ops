using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace NorthwindOps.Components.Guide.Demos.ValidationLocalization;

/// <summary>The fields both New supplier forms post.</summary>
public interface ISupplierForm
{
    string? Company { get; set; }
    string? Email { get; set; }
}

/// <summary>What the .NET 11 form posts: plain attributes, translated by the localizer.</summary>
// xray:validation-localization
// AddValidation() validates [ValidatableType] models, and translates their messages.
[ValidatableType]
// /xray:validation-localization
public sealed class NewSupplierForm : ISupplierForm
{
    // xray:validation-localization
    // No message settings on any attribute. Each rule's message is looked up by its name.
    [Required, StringLength(40), Display(Name = "Company")]
    public string? Company { get; set; }

    [Required, EmailAddress, Display(Name = "Email")]
    public string? Email { get; set; } = "sales@";
    // /xray:validation-localization
}
