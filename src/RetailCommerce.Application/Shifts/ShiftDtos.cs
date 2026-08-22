using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Shifts;

public record ShiftDto(
    Guid Id,
    string ShiftNumber,
    Guid WarehouseId,
    string WarehouseName,
    DateTimeOffset OpenedAtUtc,
    string? OpenedByName,
    DateTimeOffset? ClosedAtUtc,
    string? ClosedByName,
    string Status,
    decimal TotalSales,
    decimal TotalExpenses,
    decimal NetTotal,
    /// <summary>Echoes the Open request's idempotency key (when it carried one) — lets an offline
    /// POS's sync engine positively match this response back to the outbox row that sent it.</summary>
    Guid? ClientTransactionId);

public record ExpenseDto(
    Guid Id,
    Guid ShiftId,
    Guid ExpenseCategoryId,
    string ExpenseCategoryName,
    decimal Amount,
    string? Note,
    DateTimeOffset CreatedAtUtc,
    Guid? ClientTransactionId);

/// <summary>Live totals while a shift is Open (computed from real Orders/Expenses each call);
/// once Closed, this reflects the snapshot taken at close time.</summary>
public record ShiftSummaryDto(Guid ShiftId, decimal TotalSales, decimal TotalExpenses, decimal NetTotal, IReadOnlyList<ExpenseDto> Expenses);

public record OpenShiftRequest(
    Guid WarehouseId,
    /// <summary>Client-generated idempotency key from the POS offline outbox. Null for a caller
    /// that never goes through the outbox — OpenShiftAsync only enforces idempotency when set.</summary>
    Guid? ClientTransactionId = null);

public record AddExpenseRequest(
    Guid ExpenseCategoryId,
    decimal Amount,
    string? Note,
    Guid? ClientTransactionId = null);

/// <summary>Only meaningful when the shift is already Closed: if it matches the stored
/// CloseClientTransactionId, CloseShiftAsync treats the call as an idempotent retry instead of a
/// "shift already closed" conflict. See Shift.CloseClientTransactionId's doc comment.</summary>
public record CloseShiftRequest(Guid? ClientTransactionId = null);

public class ShiftListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public string? Status { get; set; }
}
