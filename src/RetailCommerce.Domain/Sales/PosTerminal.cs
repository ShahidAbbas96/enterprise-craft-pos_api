using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;

namespace RetailCommerce.Domain.Sales;

/// <summary>A physical till/register. Tied to exactly one Warehouse (its stockroom) rather than
/// a Store directly — Order.WarehouseId is already single-valued, so a terminal spanning
/// multiple warehouses isn't representable downstream anyway. Store is reached transitively via
/// Warehouse.Store to avoid a denormalized StoreId that could drift out of sync.</summary>
public class PosTerminal : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public ICollection<PosTerminalUser> AssignedUsers { get; set; } = new List<PosTerminalUser>();
}
