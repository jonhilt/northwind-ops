using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;

namespace NorthwindOps.Features.Suppliers;

/// <summary>
/// Supplier codes look like "EXOT-01": four capital letters, a dash and a two-digit number that isn't 00.
/// The server checks it here; <see cref="GetClientValidationRules"/> asks the browser to run the same check
/// (the "suppliercode" validator in wwwroot/js/suppliers.js), so a static SSR form flags it as you type.
/// </summary>
// xray:ssr-client-validation
[AttributeUsage(AttributeTargets.Property)]
public sealed class SupplierCodeAttribute() : ValidationAttribute("{0} must look like {3}."), IClientValidationRuleProvider
{
    public int Letters { get; set; } = 4;

    public int Digits { get; set; } = 2;

    public override bool IsValid(object? value) => value is not string { Length: > 0 } code
        || (Regex.IsMatch(code, $"^[A-Z]{{{Letters}}}-[0-9]{{{Digits}}}$") && !code.EndsWith(new string('0', Digits), StringComparison.Ordinal));

    public IEnumerable<ClientValidationRule> GetClientValidationRules() =>
    [
        new("suppliercode", new Dictionary<string, string>
        {
            ["letters"] = Letters.ToString(CultureInfo.InvariantCulture),
            ["digits"] = Digits.ToString(CultureInfo.InvariantCulture),
        }),
    ];
    // /xray:ssr-client-validation

    // xray:validation-localization
    // Fills in the message's extra values: {1} letters, {2} digits and {3} an example code like "ABCD-01".
    // .NET 11's validation calls this with the localized template (SupplierCodeAttribute_Error), on the server
    // and for the browser rule; FormatErrorMessage calls it with ErrorMessageString when nothing is localized.
    public override string FormatMessage(string format, string name) =>
        string.Format(CultureInfo.CurrentCulture, format, name, Letters, Digits, Example);

    string Example => string.Concat(Enumerable.Range(0, Letters).Select(i => (char)('A' + i % 26))) + "-" + "1".PadLeft(Digits, '0');
    // /xray:validation-localization
}
