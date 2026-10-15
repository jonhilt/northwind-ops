using System.ComponentModel.DataAnnotations;

namespace DisplayNameRepro;

// The form's model: the one place the names are supposed to live.
// Pretend the team just renamed "Main contact" to "Contact person" here.
public sealed class SupplierForm
{
    [Required, Display(Name = "Supplier code")]
    public string? Code { get; set; }

    [Required, Display(Name = "Company name")]
    public string? Company { get; set; }

    [Required, Display(Name = "Contact person")]
    public string? Contact { get; set; }
}
