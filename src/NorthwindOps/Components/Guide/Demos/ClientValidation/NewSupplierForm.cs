using System.ComponentModel.DataAnnotations;

namespace NorthwindOps.Components.Guide.Demos.ClientValidation;

/// <summary>What the "New supplier" form posts.</summary>
public sealed class NewSupplierForm
{
    // xray:ssr-client-validation
    // Plain DataAnnotations: the server checks them on every post,
    // and now the browser checks them as you type.
    [Required, StringLength(40)]
    // /xray:ssr-client-validation
    public string? Company { get; set; }

    // xray:ssr-client-validation
    [Required, EmailAddress, StringLength(60)]
    // /xray:ssr-client-validation
    public string? Email { get; set; }
}
