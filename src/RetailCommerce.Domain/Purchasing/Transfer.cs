using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;

namespace RetailCommerce.Domain.Purchasing;

public class Transfer : BaseEntity
{
    public string TransferNumber { get; set; } = default!;

    public Guid FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; } = default!;

    public Guid ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; } = default!;

    public DateOnly TransferDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Draft;

    public Guid? CreatedByUserId { get; set; }

    public ICollection<TransferLine> Lines { get; set; } = new List<TransferLine>();
}
