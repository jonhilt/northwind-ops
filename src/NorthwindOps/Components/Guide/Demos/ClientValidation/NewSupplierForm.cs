using System.ComponentModel.DataAnnotations;

namespace NorthwindOps.Components.Guide.Demos.ClientValidation;

/// <summary>What the "New supplier" form posts, on both sides.</summary>
public sealed class NewSupplierForm
{
    // xray:ssr-client-validation
    // Not new: the same attributes as .NET 10. The server checks them on every post,
    // and on .NET 11 they also become the browser's rules, with the same messages.
    [Required, StringLength(40)]
    public string? Company { get; set; }

    [Required, EmailAddress, StringLength(60)]
    // /xray:ssr-client-validation
    public string? Email { get; set; }
}
