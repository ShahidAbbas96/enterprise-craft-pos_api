using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Inventory;

/// <summary>Immutable audit ledger row. Every change to an InventoryBalance.Quantity must be
/// accompanied by one of these — never update stock without writing the movement that explains it.</summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int QuantityDelta { get; set; }
    public StockMovementKind Kind { get; set; }
    public string? Reference { get; set; }
    public Guid? PerformedByUserId { get; set; }
}
