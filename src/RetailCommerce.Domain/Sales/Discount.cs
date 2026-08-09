using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Taxonomy;

namespace RetailCommerce.Domain.Sales;

/// <summary>Named discount campaign (e.g. "14 August Sale — 15% off") managed in Settings.
/// DepartmentId and ProductId are independent, non-exclusive targets — a discount with neither
/// set is a cart-wide campaign the cashier must pick manually at POS; one with either (or both)
/// set auto-applies to any matching cart line with no cashier action, taking precedence over a
/// manual/cart-wide discount (Product match wins over Department match — see
/// SalesService.ResolveLineDiscount). Setting both lets one campaign cover, e.g., "all of
/// Footwear plus this one Bag that's on promotion too". The order only stores the resulting
/// DiscountLabel/DiscountPercent snapshot — this row is just the source of truth for resolution
/// at sale time, not a foreign key target from Order.</summary>
public class Discount : BaseEntity
{
    public string Name { get; set; } = default!;
    public DiscountValueType Type { get; set; } = DiscountValueType.Percentage;
    public decimal Value { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
}
