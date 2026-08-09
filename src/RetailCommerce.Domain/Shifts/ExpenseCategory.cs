using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Shifts;

/// <summary>Settings-managed list of expense categories (e.g. "Rent", "Utilities", "Fuel")
/// cashiers pick from when logging an expense during shift close.</summary>
public class ExpenseCategory : BaseEntity, ITaxonomyNode
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
