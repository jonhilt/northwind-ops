using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace NorthwindOps.Components.Guide.Demos.NestedValidation;

/// <summary>What the "New supplier" form posts, with an address and contacts.</summary>
// xray:validatable-type-stable
// With AddValidation() in Program.cs, all this reaches is validated. No #pragma.
[ValidatableType]
// /xray:validatable-type-stable
public sealed class NewSupplierForm
{
    [Required, StringLength(40)]
    public string? Company { get; set; }

    // xray:validatable-type-stable
    public AddressForm Address { get; set => field = value ?? new(); } = new();

    // One contact row to start. A post with no rows binds null: keep a list.
    public List<ContactForm> Contacts { get; set => field = value ?? []; } = [new()];
    // /xray:validatable-type-stable
}

// Reached through NewSupplierForm, so these need no attribute of their own.
public sealed class AddressForm
{
    [Required, StringLength(40)]
    public string? City { get; set; }
}

public sealed class ContactForm
{
    [Required, StringLength(40), Display(Name = "Contact name")]
    public string? Name { get; set; }

    [Required, EmailAddress, StringLength(60), Display(Name = "Contact email")]
    public string? Email { get; set; }
}
