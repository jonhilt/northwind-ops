using System.Globalization;
using Microsoft.Extensions.Localization;

namespace NorthwindOps.Components.Guide.Demos.ValidationLocalization;

/// <summary>
/// The .NET 11 form's German text. Validation messages are keyed by rule name; the page's own
/// text by its English, so English needs no entries. A .resx file or a database table works too.
/// </summary>
public sealed class SupplierMessages : IStringLocalizer
{
    static readonly Dictionary<string, string> German = new()
    {
        // xray:validation-localization
        // One key per kind of rule, for every property that uses it. {0} is the display name.
        ["RequiredAttribute_Error"] = "{0} ist ein Pflichtfeld.",
        ["StringLengthAttribute_Error"] = "{0} darf höchstens {1} Zeichen lang sein.",
        ["EmailAddressAttribute_Error"] = "{0} ist keine gültige E-Mail-Adresse.",
        // Display names, looked up the same way
        ["Company"] = "Firma",
        ["Email"] = "E-Mail",
        // /xray:validation-localization
        ["New supplier"] = "Neuer Lieferant",
        ["Save supplier"] = "Lieferant speichern",
        ["Saved"] = "Gespeichert",
    };

    // Not found? Validation falls back to the attribute's own English message.
    public LocalizedString this[string name] =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de"
            && German.TryGetValue(name, out var text)
            ? new(name, text)
            : new(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(CultureInfo.CurrentCulture, this[name].Value, arguments));

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        German.Keys.Select(key => this[key]);
}
