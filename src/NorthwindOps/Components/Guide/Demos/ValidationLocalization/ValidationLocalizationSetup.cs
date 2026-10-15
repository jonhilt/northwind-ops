using Microsoft.Extensions.Validation;

namespace NorthwindOps.Components.Guide.Demos.ValidationLocalization;

public static class ValidationLocalizationSetup
{
    /// <summary>Called from Program.cs, after AddLocalization() and AddValidation().</summary>
    public static IServiceCollection AddSupplierMessages(this IServiceCollection services)
    {
        var messages = new SupplierMessages();
        services.AddSingleton(messages); // the form's labels use it too

        // xray:validation-localization
        // New in .NET 11: validation asks a localizer for each message. This sends the
        // .NET 11 form's to SupplierMessages, and leaves every other type as it was.
        services.Configure<ValidationOptions>(options =>
        {
            var fallback = options.LocalizerProvider;
            options.LocalizerProvider = (type, factory) =>
                type == typeof(NewSupplierForm) ? messages : fallback(type, factory);
        });
        // /xray:validation-localization
        return services;
    }
}
