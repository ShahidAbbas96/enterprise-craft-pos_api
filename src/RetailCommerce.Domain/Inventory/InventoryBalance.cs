using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Inventory;

/// <summary>Per-warehouse stock balance for a product. This is the single source of truth for
/// stock on hand; a product's total stock is always a query (SUM) over this table, never a
/// separately-stored, sync-triggered column (the original prototype used a DB trigger for that;
/// we compute it instead to avoid the extra moving part).</summary>
public class InventoryBalance : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int Quantity { get; set; }
}
