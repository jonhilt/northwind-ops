using System.ComponentModel.DataAnnotations;
#if USING
using Microsoft.Extensions.Validation;
#endif

namespace NestedRepro;

// /plain: no attribute. DataAnnotationsValidator only sees Company.
public class PlainSupplierForm
{
    [Required] public string? Company { get; set; }
    public AddressForm Address { get; set => field = value ?? new(); } = new();
    public List<ContactForm> Contacts { get; set => field = value ?? []; } = [new()];
}

// /marked: nested validation in .NET 10. Build variants (dotnet build -p:DefineConstants=...):
//   (none)          [ValidatableType] binds to the SDK-generated internal
//                   Microsoft.Extensions.Validation.Embedded.ValidatableTypeAttribute: no diagnostic at all.
//   PUBLIC          the public, [Experimental] attribute, with the #pragma: builds.
//   PUBLIC%3BNOPRAGMA  the public attribute without the #pragma: error ASP0029.
//   USING           `using Microsoft.Extensions.Validation;` + [ValidatableType]: error CS0104 (ambiguous).
#if PUBLIC && !NOPRAGMA
#pragma warning disable ASP0029 // experimental in .NET 10
#endif
#if PUBLIC
[Microsoft.Extensions.Validation.ValidatableType]
#else
[ValidatableType]
#endif
#if PUBLIC && !NOPRAGMA
#pragma warning restore ASP0029
#endif
public class MarkedSupplierForm
{
    [Required] public string? Company { get; set; }
    public AddressForm Address { get; set => field = value ?? new(); } = new();
    public List<ContactForm> Contacts { get; set => field = value ?? []; } = [new()];
}

public class AddressForm
{
    [Required] public string? City { get; set; }
}

public class ContactForm
{
    [Required] public string? Name { get; set; }
    [Required, EmailAddress] public string? Email { get; set; }
}
