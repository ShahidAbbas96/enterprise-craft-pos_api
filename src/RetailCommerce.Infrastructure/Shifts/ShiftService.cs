using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Shifts;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Shifts;
using RetailCommerce.Domain.Sync;
using RetailCommerce.Infrastructure.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Shifts;

public class ShiftService(AppDbContext db, IDocumentNumberService documentNumbers, ICurrentUserService currentUser) : IShiftService
{
    public async Task<ShiftDto?> GetOpenShiftAsync(Guid warehouseId, CancellationToken ct = default)
    {
        var scopedWarehouseId = currentUser.ResolveWarehouseScope(warehouseId);
        var shift = await Query().FirstOrDefaultAsync(s => s.WarehouseId == scopedWarehouseId && s.Status == ShiftStatus.Open, ct);
        if (shift is null) return null;
        var names = await GetUserNamesAsync([shift], ct);
        return await ToDtoWithLiveTotalsAsync(shift, names, ct);
    }

    public async Task<ShiftDto> OpenShiftAsync(OpenShiftRequest request, Guid? userId, CancellationToken ct = default)
    {
        // Fast-path idempotency pre-check — mirrors SalesService.CreateSaleAsync exactly. The real
        // safety net is the filtered unique index on Shift.ClientTransactionId caught below.
        if (request.ClientTransactionId is { } precheckKey)
        {
            var existingShiftId = await db.Shifts
                .Where(s => s.ClientTransactionId == precheckKey)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);
            if (existingShiftId is { } duplicateShiftId)
            {
                await LogSyncAsync("Shift", duplicateShiftId, precheckKey, SyncLogStatus.Duplicate, null, ct);
                return await GetAsync(duplicateShiftId, ct);
            }
        }

        var warehouseId = currentUser.ResolveWarehouseScope(request.WarehouseId);
        if (!await db.Warehouses.AnyAsync(w => w.Id == warehouseId, ct))
        {
            throw new NotFoundException("Warehouse", warehouseId);
        }
        // Genuine business conflict (not an idempotency retry) — e.g. two offline terminals for
        // the same store each opened a shift while disconnected. Per policy this is never
        // auto-merged: it surfaces as a ConflictException, which the offline sync engine marks
        // the outbox item Failed for manual back-office reconciliation rather than retrying.
        if (await db.Shifts.AnyAsync(s => s.WarehouseId == warehouseId && s.Status == ShiftStatus.Open, ct))
        {
            if (request.ClientTransactionId is { } cta)
            {
                await LogSyncAsync("Shift", null, cta, SyncLogStatus.Failed, "Another shift is already open for this warehouse.", ct);
            }
            throw new ConflictException("A shift is already open for this warehouse.");
        }

        var shift = new Shift
        {
            ShiftNumber = await documentNumbers.NextAsync(DocumentType.Shift, ct: ct),
            WarehouseId = warehouseId,
            OpenedByUserId = userId,
            OpenedAtUtc = DateTimeOffset.UtcNow,
            Status = ShiftStatus.Open,
            ClientTransactionId = request.ClientTransactionId,
        };
        db.Shifts.Add(shift);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolationOn("IX_Shifts_ClientTransactionId"))
        {
            // Real safety net: a concurrent retry of the same offline Open raced this one past the
            // fast-path pre-check above — same recovery pattern as SalesService.CreateSaleAsync.
            db.ChangeTracker.Clear();
            var winningShiftId = await db.Shifts
                .Where(s => s.ClientTransactionId == request.ClientTransactionId)
                .Select(s => s.Id)
                .FirstAsync(ct);
            await LogSyncAsync("Shift", winningShiftId, request.ClientTransactionId, SyncLogStatus.Duplicate, null, ct);
            return await GetAsync(winningShiftId, ct);
        }

        if (request.ClientTransactionId is not null)
        {
            await LogSyncAsync("Shift", shift.Id, request.ClientTransactionId, SyncLogStatus.Success, null, ct);
        }

        return await GetAsync(shift.Id, ct);
    }

    public async Task<ShiftSummaryDto> GetSummaryAsync(Guid shiftId, CancellationToken ct = default)
    {
        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId, ct) ?? throw new NotFoundException("Shift", shiftId);
        currentUser.ResolveWarehouseScope(shift.WarehouseId);
        var (totalSales, totalExpenses) = await ComputeLiveTotalsAsync(shift, ct);
        var expenses = await GetExpenseDtosAsync(shiftId, ct);
        return new ShiftSummaryDto(shift.Id, totalSales, totalExpenses, totalSales - totalExpenses, expenses);
    }

    public async Task<ExpenseDto> AddExpenseAsync(Guid shiftId, AddExpenseRequest request, Guid? userId, CancellationToken ct = default)
    {
        if (request.ClientTransactionId is { } precheckKey)
        {
            var existing = await db.Expenses
                .Where(e => e.ClientTransactionId == precheckKey)
                .Include(e => e.ExpenseCategory)
                .FirstOrDefaultAsync(ct);
            if (existing is not null)
            {
                await LogSyncAsync("Expense", existing.Id, precheckKey, SyncLogStatus.Duplicate, null, ct);
                return ToExpenseDto(existing);
            }
        }

        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId, ct) ?? throw new NotFoundException("Shift", shiftId);
        currentUser.ResolveWarehouseScope(shift.WarehouseId);
        if (shift.Status != ShiftStatus.Open)
        {
            throw new ConflictException("Cannot add an expense to a closed shift.");
        }
        if (!await db.ExpenseCategories.AnyAsync(c => c.Id == request.ExpenseCategoryId, ct))
        {
            throw new NotFoundException("ExpenseCategory", request.ExpenseCategoryId);
        }

        var expense = new Expense
        {
            ShiftId = shiftId,
            ExpenseCategoryId = request.ExpenseCategoryId,
            Amount = request.Amount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedByUserId = userId,
            ClientTransactionId = request.ClientTransactionId,
        };
        db.Expenses.Add(expense);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolationOn("IX_Expenses_ClientTransactionId"))
        {
            db.ChangeTracker.Clear();
            var winning = await db.Expenses.Include(e => e.ExpenseCategory)
                .FirstAsync(e => e.ClientTransactionId == request.ClientTransactionId, ct);
            await LogSyncAsync("Expense", winning.Id, request.ClientTransactionId, SyncLogStatus.Duplicate, null, ct);
            return ToExpenseDto(winning);
        }

        if (request.ClientTransactionId is not null)
        {
            await LogSyncAsync("Expense", expense.Id, request.ClientTransactionId, SyncLogStatus.Success, null, ct);
        }

        var category = await db.ExpenseCategories.FirstAsync(c => c.Id == request.ExpenseCategoryId, ct);
        return new ExpenseDto(expense.Id, expense.ShiftId, expense.ExpenseCategoryId, category.Name, expense.Amount, expense.Note, expense.CreatedAtUtc, expense.ClientTransactionId);
    }

    /// <summary>Unlike Open/AddExpense, Close is an UPDATE to an existing row rather than an
    /// INSERT, so it can't lean on a filtered-unique-index race guard the same way. Instead: if
    /// the shift is already Closed AND its stored CloseClientTransactionId matches the incoming
    /// request's, this is treated as an idempotent retry (return the current state) rather than a
    /// "shift already closed" conflict — exactly the distinction the approved plan called for.
    /// Already-closed with a *different* (or absent) key is still a genuine conflict.</summary>
    public async Task<ShiftDto> CloseShiftAsync(Guid shiftId, CloseShiftRequest request, Guid? userId, CancellationToken ct = default)
    {
        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId, ct) ?? throw new NotFoundException("Shift", shiftId);
        currentUser.ResolveWarehouseScope(shift.WarehouseId);

        if (shift.Status != ShiftStatus.Open)
        {
            if (request.ClientTransactionId is { } retryKey && shift.CloseClientTransactionId == retryKey)
            {
                await LogSyncAsync("ShiftClose", shift.Id, retryKey, SyncLogStatus.Duplicate, null, ct);
                return await GetAsync(shift.Id, ct);
            }
            throw new ConflictException("This shift is already closed.");
        }

        var (totalSales, totalExpenses) = await ComputeLiveTotalsAsync(shift, ct);
        shift.TotalSales = totalSales;
        shift.TotalExpenses = totalExpenses;
        shift.NetTotal = totalSales - totalExpenses;
        shift.Status = ShiftStatus.Closed;
        shift.ClosedByUserId = userId;
        shift.ClosedAtUtc = DateTimeOffset.UtcNow;
        shift.CloseClientTransactionId = request.ClientTransactionId;
        await db.SaveChangesAsync(ct);

        if (request.ClientTransactionId is not null)
        {
            await LogSyncAsync("ShiftClose", shift.Id, request.ClientTransactionId, SyncLogStatus.Success, null, ct);
        }

        return await GetAsync(shift.Id, ct);
    }

    public async Task<ShiftDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var shift = await Query().FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Shift", id);
        currentUser.ResolveWarehouseScope(shift.WarehouseId);
        var names = await GetUserNamesAsync([shift], ct);
        return await ToDtoWithLiveTotalsAsync(shift, names, ct);
    }

    public async Task<PagedResult<ShiftDto>> ListAsync(ShiftListQuery query, CancellationToken ct = default)
    {
        var shifts = Query();
        if (currentUser.ResolveWarehouseScope(query.WarehouseId) is { } wh) shifts = shifts.Where(s => s.WarehouseId == wh);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ShiftStatus>(query.Status, true, out var status))
        {
            shifts = shifts.Where(s => s.Status == status);
        }

        var totalCount = await shifts.CountAsync(ct);
        var page = await shifts
            .OrderByDescending(s => s.OpenedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var names = await GetUserNamesAsync(page, ct);
        var items = new List<ShiftDto>();
        foreach (var shift in page)
        {
            items.Add(await ToDtoWithLiveTotalsAsync(shift, names, ct));
        }

        return new PagedResult<ShiftDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    private IQueryable<Shift> Query() => db.Shifts.Include(s => s.Warehouse);

    private async Task<(decimal TotalSales, decimal TotalExpenses)> ComputeLiveTotalsAsync(Shift shift, CancellationToken ct)
    {
        var windowEnd = shift.ClosedAtUtc ?? DateTimeOffset.UtcNow;
        var totalSales = await db.Orders
            .Where(o => o.WarehouseId == shift.WarehouseId && o.Status == OrderStatus.Completed &&
                        o.CreatedAtUtc >= shift.OpenedAtUtc && o.CreatedAtUtc <= windowEnd)
            .SumAsync(o => (decimal?)o.Total, ct) ?? 0;
        var totalExpenses = await db.Expenses.Where(e => e.ShiftId == shift.Id).SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        return (totalSales, totalExpenses);
    }

    private async Task<ShiftDto> ToDtoWithLiveTotalsAsync(Shift shift, IReadOnlyDictionary<Guid, string> names, CancellationToken ct)
    {
        var (totalSales, totalExpenses) = shift.Status == ShiftStatus.Closed
            ? (shift.TotalSales, shift.TotalExpenses)
            : await ComputeLiveTotalsAsync(shift, ct);

        return new ShiftDto(
            shift.Id, shift.ShiftNumber, shift.WarehouseId, shift.Warehouse.Name,
            shift.OpenedAtUtc, shift.OpenedByUserId.HasValue && names.TryGetValue(shift.OpenedByUserId.Value, out var openedBy) ? openedBy : null,
            shift.ClosedAtUtc, shift.ClosedByUserId.HasValue && names.TryGetValue(shift.ClosedByUserId.Value, out var closedBy) ? closedBy : null,
            shift.Status.ToString(), totalSales, totalExpenses, totalSales - totalExpenses, shift.ClientTransactionId);
    }

    private static ExpenseDto ToExpenseDto(Expense e) =>
        new(e.Id, e.ShiftId, e.ExpenseCategoryId, e.ExpenseCategory.Name, e.Amount, e.Note, e.CreatedAtUtc, e.ClientTransactionId);

    private async Task<List<ExpenseDto>> GetExpenseDtosAsync(Guid shiftId, CancellationToken ct) =>
        await db.Expenses.Include(e => e.ExpenseCategory)
            .Where(e => e.ShiftId == shiftId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new ExpenseDto(e.Id, e.ShiftId, e.ExpenseCategoryId, e.ExpenseCategory.Name, e.Amount, e.Note, e.CreatedAtUtc, e.ClientTransactionId))
            .ToListAsync(ct);

    private async Task LogSyncAsync(string entityType, Guid? entityId, Guid? clientTransactionId, SyncLogStatus status, string? errorMessage, CancellationToken ct)
    {
        db.SyncLogs.Add(new SyncLog
        {
            TerminalId = currentUser.TerminalId,
            Direction = SyncDirection.Push,
            EntityType = entityType,
            EntityId = entityId,
            ClientTransactionId = clientTransactionId,
            Status = status,
            ErrorMessage = errorMessage,
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyList<Shift> shifts, CancellationToken ct)
    {
        var userIds = shifts
            .SelectMany(s => new[] { s.OpenedByUserId, s.ClosedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (userIds.Count == 0) return new Dictionary<Guid, string>();

        return await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName}{(u.LastName is { Length: > 0 } ln ? " " + ln : "")}", ct);
    }
}
