using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace NorthwindOps.Features.Suppliers;

/// <summary>
/// The Suppliers screen's text in English (SupplierText.resx) and German (SupplierText.de.resx).
/// <para>
/// Validation messages are read through <c>IStringLocalizer</c> by the .NET 11 validation pipeline, using
/// keys like <c>RequiredAttribute_Error</c>. Field names are exposed here as static properties so that
/// <c>[Display(ResourceType = typeof(SupplierText))]</c> can use them: that's what <c>Label</c> and
/// <c>DisplayName</c> read, and the validation messages use the same names.
/// </para>
/// </summary>
public sealed class SupplierText
{
    // Not static, because it's also the type argument of IStringLocalizer<SupplierText>.
    SupplierText() { }

    static readonly ResourceManager Resources = new(typeof(SupplierText));

    // xray:validation-localization
    public static string Company => Get();
    public static string Code => Get();
    public static string Contact => Get();
    public static string Email => Get();
    public static string Phone => Get();
    public static string Website => Get();
    public static string VatNumber => Get();
    public static string Street => Get();
    public static string City => Get();
    public static string PostalCode => Get();
    public static string Country => Get();
    public static string ContactName => Get();
    public static string ContactRole => Get();
    public static string ContactEmail => Get();

    static string Get([CallerMemberName] string key = "") => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    // /xray:validation-localization
}
