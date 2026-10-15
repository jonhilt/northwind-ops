using System.ComponentModel.DataAnnotations;
// xray:validatable-type-stable
using Microsoft.Extensions.Validation; // where [ValidatableType] lives
// /xray:validatable-type-stable

namespace NorthwindOps.Components.Guide.Demos.NestedValidation;

/// <summary>The fields both New supplier forms post: a company, its address and its contacts.</summary>
public interface ISupplierForm
{
    string? Company { get; set; }
    AddressForm Address { get; set; }
    List<ContactForm> Contacts { get; set; }
}

// The unmarked model. Nothing marks it, so validation stops at Company.
public sealed class SupplierDraft : ISupplierForm
{
    [Required, StringLength(40)] public string? Company { get; set; }
    public AddressForm Address { get; set => field = value ?? new(); } = new();
    public List<ContactForm> Contacts { get; set => field = value ?? []; } = [new(), new()];
}

// xray:validatable-type-stable
// The marked model: the same fields, marked so validation walks everything
// they reach. The attribute is the only difference between the two models.
[ValidatableType]
// /xray:validatable-type-stable
public sealed class NewSupplierForm : ISupplierForm
{
    [Required, StringLength(40)] public string? Company { get; set; }
    public AddressForm Address { get; set => field = value ?? new(); } = new();
    public List<ContactForm> Contacts { get; set => field = value ?? []; } = [new(), new()];
}

// Reached through the marked type's properties, so these need no attribute of their own.
public sealed class AddressForm
{
    [Required, StringLength(40)]
    public string? City { get; set; }
}

public sealed class ContactForm
{
    [Required, StringLength(40), Display(Name = "Name")]
    public string? Name { get; set; }

    [Required, EmailAddress, StringLength(60), Display(Name = "Email")]
    public string? Email { get; set; }
}
