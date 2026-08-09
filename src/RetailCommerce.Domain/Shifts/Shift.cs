using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;

namespace RetailCommerce.Domain.Shifts;

/// <summary>One shift per warehouse/store per day — only one may be Open at a time for a given
/// warehouse. TotalSales/TotalExpenses/NetTotal are snapshotted at close time from real Order/
/// Expense rows; while Open they're computed live instead (see IShiftService.GetSummaryAsync).</summary>
public class Shift : BaseEntity
{
    public string ShiftNumber { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public Guid? OpenedByUserId { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? ClosedByUserId { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetTotal { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
