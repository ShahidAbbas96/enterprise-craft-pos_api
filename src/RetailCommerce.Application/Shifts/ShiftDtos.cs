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
    decimal NetTotal);

public record ExpenseDto(
    Guid Id,
    Guid ShiftId,
    Guid ExpenseCategoryId,
    string ExpenseCategoryName,
    decimal Amount,
    string? Note,
    DateTimeOffset CreatedAtUtc);

/// <summary>Live totals while a shift is Open (computed from real Orders/Expenses each call);
/// once Closed, this reflects the snapshot taken at close time.</summary>
public record ShiftSummaryDto(Guid ShiftId, decimal TotalSales, decimal TotalExpenses, decimal NetTotal, IReadOnlyList<ExpenseDto> Expenses);

public record OpenShiftRequest(Guid WarehouseId);

public record AddExpenseRequest(Guid ExpenseCategoryId, decimal Amount, string? Note);

public class ShiftListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public string? Status { get; set; }
}
