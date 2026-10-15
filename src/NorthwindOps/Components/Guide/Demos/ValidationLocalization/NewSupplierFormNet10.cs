using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace NorthwindOps.Components.Guide.Demos.ValidationLocalization;

// xray:validation-localization
// The .NET 10 way: validation never asked a localizer, so every attribute points at a resource.
public sealed class NewSupplierFormNet10 : ISupplierForm
{
    [Required(ErrorMessageResourceType = typeof(Net10Messages), ErrorMessageResourceName = "Required")]
    [StringLength(40, ErrorMessageResourceType = typeof(Net10Messages), ErrorMessageResourceName = "TooLong")]
    [Display(ResourceType = typeof(Net10Messages), Name = "Company")]
    public string? Company { get; set; }

    [Required(ErrorMessageResourceType = typeof(Net10Messages), ErrorMessageResourceName = "Required")]
    [EmailAddress] // missed one: it keeps its own English message
    [Display(ResourceType = typeof(Net10Messages), Name = "Email")]
    public string? Email { get; set; } = "sales@";
}
// /xray:validation-localization

/// <summary>The resource those attributes read: one static property per message, in either language.</summary>
public static class Net10Messages
{
    static bool German => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";

    public static string Required => German ? "{0} ist ein Pflichtfeld." : "The {0} field is required.";
    public static string TooLong => German ? "{0} darf höchstens {1} Zeichen lang sein." : "{0} is too long.";
    public static string Company => German ? "Firma" : "Company";
    public static string Email => German ? "E-Mail" : "Email";
}
