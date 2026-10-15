using CultureRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddLocalization(); // in .NET 11 this alone would switch on "culture from server"

var app = builder.Build();

// ?culture=de-DE sets the request's culture, as in any localized app.
app.UseRequestLocalization(o =>
{
    o.AddSupportedCultures("en-US", "de-DE", "fr-FR").AddSupportedUICultures("en-US", "de-DE", "fr-FR").SetDefaultCulture("en-US");
    o.RequestCultureProviders = [new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider()];
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CultureRepro.Client._Imports).Assembly);
app.Run();
