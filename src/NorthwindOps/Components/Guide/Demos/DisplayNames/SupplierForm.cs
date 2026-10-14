using System.ComponentModel.DataAnnotations;
using NorthwindOps.Features.Suppliers;

namespace NorthwindOps.Components.Guide.Demos.DisplayNames;

/// <summary>
/// The "New supplier" form's model, cut down. Its names live in SupplierText.resx,
/// with German in SupplierText.de.resx.
/// </summary>
public sealed class SupplierForm
{
    // xray:displayname-component
    [Display(Name = nameof(SupplierText.Code), ResourceType = typeof(SupplierText))]
    public string? Code { get; set; }

    [Display(Name = nameof(SupplierText.Company), ResourceType = typeof(SupplierText))]
    public string? Company { get; set; }

    [Display(Name = nameof(SupplierText.Contact), ResourceType = typeof(SupplierText))]
    public string? Contact { get; set; }

    [Display(Name = nameof(SupplierText.Country), ResourceType = typeof(SupplierText))]
    public string? Country { get; set; }
    // /xray:displayname-component
}
