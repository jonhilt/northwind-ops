using System.ComponentModel.DataAnnotations;
using NorthwindOps.Features.Suppliers;

namespace NorthwindOps.Components.Guide.Demos.DisplayNames;

/// <summary>
/// The "New supplier" form's model, cut down: the one place each field's name is set.
/// </summary>
public sealed class SupplierForm
{
    // xray:displayname-component
    // Not new: [Display] names, read by the form's labels and validation messages.
    // ResourceType makes each Name a key, looked up in the request's UI culture,
    // so ?culture=de-DE picks the German file:
    //   SupplierText.resx      Contact = "Main contact"
    //   SupplierText.de.resx   Contact = "Hauptansprechpartner"
    [Display(Name = nameof(SupplierText.Code), ResourceType = typeof(SupplierText))]
    public string? Code { get; set; }

    [Display(Name = nameof(SupplierText.Company), ResourceType = typeof(SupplierText))]
    public string? Company { get; set; }

    [Display(Name = nameof(SupplierText.Contact), ResourceType = typeof(SupplierText))]
    public string? Contact { get; set; }
    // /xray:displayname-component
}
