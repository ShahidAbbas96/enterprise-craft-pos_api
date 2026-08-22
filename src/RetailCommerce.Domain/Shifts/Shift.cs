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

    /// <summary>Client-generated idempotency key from the POS offline outbox for the Open action —
    /// same pattern as Order.ClientTransactionId. Null for a shift opened directly online.</summary>
    public Guid? ClientTransactionId { get; set; }

    /// <summary>Separate idempotency key for the Close action — a close is an UPDATE to this same
    /// row rather than an INSERT, so it can't reuse the same unique-index trick as Open/ClientTransactionId;
    /// CloseShiftAsync instead compares this against an incoming retry's key directly (see its
    /// doc comment) to tell "already-closed, matching retry" apart from a genuine conflict.</summary>
    public Guid? CloseClientTransactionId { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
