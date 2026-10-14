using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace NorthwindOps.Features.Suppliers;

/// <summary>
/// A draft skips validation, but not the size rules: SQLite doesn't enforce column lengths, and the demo database is
/// shared, so without these a draft could store text of any length. Finds every [StringLength] and [MaxLength] a draft breaks.
/// </summary>
public static class DraftLimits
{
    public sealed record Broken(FieldIdentifier Field, string Name, int Max, bool IsList);

    public static IEnumerable<Broken> Check(SupplierForm form) =>
        Check((object)form).Concat(Check(form.Address)).Concat(form.Contacts.SelectMany(Check));

    static IEnumerable<Broken> Check(object model) =>
        from property in model.GetType().GetProperties()
        let value = property.GetValue(model)
        let name = property.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? property.Name
        from rule in property.GetCustomAttributes<ValidationAttribute>()
        let max = rule switch
        {
            StringLengthAttribute length => length.MaximumLength,
            MaxLengthAttribute count => count.Length,
            _ => (int?)null, // the rest a draft skips, the database and VAT register checks among them
        }
        where max is not null && !rule.IsValid(value)
        select new Broken(new(model, property.Name), name, max.Value, IsList: rule is MaxLengthAttribute);
}
