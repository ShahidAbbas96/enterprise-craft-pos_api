using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Shifts;

/// <summary>One shift per warehouse/store per day. Cashiers open a shift, log expenses against
/// it through the day, and close it to snapshot Total Sales / Total Expenses / Net — the shift
/// history list (ListAsync) is itself the "sale vs expense" report the client asked for.</summary>
public interface IShiftService
{
    Task<ShiftDto?> GetOpenShiftAsync(Guid warehouseId, CancellationToken ct = default);
    Task<ShiftDto> OpenShiftAsync(OpenShiftRequest request, Guid? userId, CancellationToken ct = default);
    Task<ShiftSummaryDto> GetSummaryAsync(Guid shiftId, CancellationToken ct = default);
    Task<ExpenseDto> AddExpenseAsync(Guid shiftId, AddExpenseRequest request, Guid? userId, CancellationToken ct = default);
    Task<ShiftDto> CloseShiftAsync(Guid shiftId, Guid? userId, CancellationToken ct = default);
    Task<ShiftDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ShiftDto>> ListAsync(ShiftListQuery query, CancellationToken ct = default);
}
