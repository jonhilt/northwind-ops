using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace NorthwindOps.Features.Suppliers;

/// <summary>
/// What the "New supplier" form posts. Field names come from SupplierText via [Display], so the labels,
/// the grid headers on the list and the validation messages all say the same thing, in English or German.
/// </summary>
// xray:validatable-type-stable
[ValidatableType] // the validation source generator walks from here into Address and Contacts
// /xray:validatable-type-stable
public sealed class SupplierForm
{
    public const int MaxContacts = 10;

    // xray:label-and-input-ids
    [Required, StringLength(40)]
    [Display(Name = nameof(SupplierText.Company), ResourceType = typeof(SupplierText))]
    public string? Company { get; set; }
    // /xray:label-and-input-ids

    // xray:ssr-client-validation
    [Required, SupplierCode]
    // /xray:ssr-client-validation
    // xray:async-form-validation
    [UniqueSupplierCode]
    // /xray:async-form-validation
    [Display(Name = nameof(SupplierText.Code), ResourceType = typeof(SupplierText))]
    public string? Code { get; set; }

    [Required, StringLength(30)]
    [Display(Name = nameof(SupplierText.Contact), ResourceType = typeof(SupplierText))]
    public string? Contact { get; set; }

    [Required, EmailAddress, StringLength(60)]
    [Display(Name = nameof(SupplierText.Email), ResourceType = typeof(SupplierText))]
    public string? Email { get; set; }

    [Phone, StringLength(24)]
    [Display(Name = nameof(SupplierText.Phone), ResourceType = typeof(SupplierText))]
    public string? Phone { get; set => field = string.IsNullOrWhiteSpace(value) ? null : value; }

    [Url, StringLength(80)]
    [Display(Name = nameof(SupplierText.Website), ResourceType = typeof(SupplierText))]
    public string? Website { get; set => field = string.IsNullOrWhiteSpace(value) ? null : value; }

    // xray:validation-localization
    // Its pattern message has a key of its own: SupplierForm_VatNumber_RegularExpressionAttribute_Error.
    [RegularExpression("^[A-Z]{2}[0-9A-Z]{8,12}$")]
    // /xray:validation-localization
    // xray:async-form-validation
    [RegisteredVatNumber]
    // /xray:async-form-validation
    [Display(Name = nameof(SupplierText.VatNumber), ResourceType = typeof(SupplierText))]
    public string? VatNumber { get; set; }

    // xray:validatable-type-stable
    public SupplierAddressForm Address { get; set => field = value ?? new(); } = new();

    // A static form that posts no contacts binds this as null, so keep it an empty list instead.
    [MaxLength(MaxContacts)]
    public List<SupplierContactForm> Contacts { get; set => field = value ?? []; } = [];
    // /xray:validatable-type-stable
}

// xray:validatable-type-stable
public sealed class SupplierAddressForm
{
    [Required, StringLength(60)]
    [Display(Name = nameof(SupplierText.Street), ResourceType = typeof(SupplierText))]
    public string? Street { get; set; }

    [Required, StringLength(40)]
    [Display(Name = nameof(SupplierText.City), ResourceType = typeof(SupplierText))]
    public string? City { get; set; }

    [Required, StringLength(10)]
    [Display(Name = nameof(SupplierText.PostalCode), ResourceType = typeof(SupplierText))]
    public string? PostalCode { get; set; }

    [Required, StringLength(40)]
    [Display(Name = nameof(SupplierText.Country), ResourceType = typeof(SupplierText))]
    public string? Country { get; set; }
}

public sealed class SupplierContactForm
{
    [Required, StringLength(40)]
    [Display(Name = nameof(SupplierText.ContactName), ResourceType = typeof(SupplierText))]
    public string? Name { get; set; }

    [StringLength(30)]
    [Display(Name = nameof(SupplierText.ContactRole), ResourceType = typeof(SupplierText))]
    public string? Role { get; set; }

    [EmailAddress, StringLength(60)]
    [Display(Name = nameof(SupplierText.ContactEmail), ResourceType = typeof(SupplierText))]
    public string? Email { get; set => field = string.IsNullOrWhiteSpace(value) ? null : value; }
}
// /xray:validatable-type-stable
