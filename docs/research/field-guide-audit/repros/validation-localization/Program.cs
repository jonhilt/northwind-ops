using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;
using ValLoc10.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddLocalization();
builder.Services.AddValidation();          // .NET 10's new validation pipeline (Microsoft.Extensions.Validation)
builder.Services.AddSingleton<IStringLocalizer, GermanMessages>();
// In .NET 11 you would now write:
//   builder.Services.Configure<ValidationOptions>(o => o.LocalizerProvider = (type, f) => new GermanMessages());
// In .NET 10, ValidationOptions has no LocalizerProvider; nothing ever asks a localizer for a message.

var app = builder.Build();
app.UseRequestLocalization(o =>
{
    o.SetDefaultCulture("en-US").AddSupportedCultures("en-US", "de-DE").AddSupportedUICultures("en-US", "de-DE");
});
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.Run();

// The .NET 11 approach: name each message after its rule. .NET 10 never looks these up.
public sealed class GermanMessages : IStringLocalizer
{
    static readonly Dictionary<string, string> German = new()
    {
        ["RequiredAttribute_Error"] = "{0} ist ein Pflichtfeld.",
        ["EmailAddressAttribute_Error"] = "{0} ist keine gültige E-Mail-Adresse.",
    };
    public LocalizedString this[string name] =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" && German.TryGetValue(name, out var t)
            ? new(name, t) : new(name, name, resourceNotFound: true);
    public LocalizedString this[string name, params object[] args] => new(name, string.Format(this[name].Value, args));
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => German.Keys.Select(k => this[k]);
}

// Form A: plain attributes, as in the .NET 11 demo.
#pragma warning disable ASP0029 // [ValidatableType] is experimental in .NET 10
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
public sealed class PlainForm
{
    [Required, Display(Name = "Firma")] public string? Company { get; set; }
    [Required, EmailAddress, Display(Name = "E-Mail")] public string? Email { get; set; }
}

// Form B: the .NET 10 workaround, pointing each attribute at a resource. One attribute was missed.
public sealed class ResourceForm
{
    [Required(ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.Required)), Display(Name = "Firma")]
    public string? Company { get; set; }

    [Required(ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.Required))]
    [EmailAddress]   // <- forgot the resource settings here
    [Display(Name = "E-Mail")]
    public string? Email { get; set; }
}

// Stand-in for a .resx designer class: static string properties that read the current UI culture.
public static class Messages
{
    public static string Required => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "{0} ist ein Pflichtfeld." : "The {0} field is required.";
}
