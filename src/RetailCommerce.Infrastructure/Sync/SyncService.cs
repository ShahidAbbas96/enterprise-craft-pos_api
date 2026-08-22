using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Customers;
using RetailCommerce.Application.Discounts;
using RetailCommerce.Application.Employees;
using RetailCommerce.Application.Products;
using RetailCommerce.Application.Sales;
using RetailCommerce.Application.Settings;
using RetailCommerce.Application.Shifts;
using RetailCommerce.Application.Sync;
using RetailCommerce.Application.Taxonomy;
using RetailCommerce.Domain.Sync;
using RetailCommerce.Infrastructure.Persistence;
using RetailCommerce.Infrastructure.Sales;

namespace RetailCommerce.Infrastructure.Sync;

/// <summary>Feeds an offline-first POS terminal's local Dexie database. Reuses every existing
/// list service (Products/Customers/Discounts/Employees/ExpenseCategories/Taxonomy/Settings)
/// rather than duplicating their query/DTO logic — the only new capability is the optional
/// UpdatedSince filter added to Product/Customer/Discount for the delta-pull path.</summary>
public class SyncService(
    AppDbContext db,
    IProductService productService,
    ICustomerService customerService,
    IDiscountService discountService,
    IEmployeeService employeeService,
    IExpenseCategoryService expenseCategoryService,
    ITaxonomyService taxonomyService,
    IPosSettingsService posSettingsService,
    IBarcodeService barcodeService,
    ICurrencySettingsService currencySettingsService,
    ICurrentUserService currentUser) : ISyncService
{
    public Task<SyncSnapshotDto> BootstrapAsync(CancellationToken ct = default) => BuildSnapshotAsync(since: null, ct);

    public Task<SyncSnapshotDto> PullAsync(DateTimeOffset? since, CancellationToken ct = default) => BuildSnapshotAsync(since, ct);

    private async Task<SyncSnapshotDto> BuildSnapshotAsync(DateTimeOffset? since, CancellationToken ct)
    {
        var entityType = since is null ? "Bootstrap" : "Pull";

        try
        {
            // A back-office (non-terminal) token has nothing to scope a POS snapshot to — sync is
            // fundamentally a terminal operation, so this fails loudly rather than returning an
            // arbitrary/unrestricted dataset.
            var warehouseId = currentUser.ResolveWarehouseScope((Guid?)null)
                ?? throw new ConflictException("A POS terminal must be selected to synchronize.");

            // Captured before any query below runs, so it's a safe "everything up to this instant
            // is included" cursor for the caller's *next* pull — the server's own clock, never the
            // client's, so there's no clock-skew bug between the two.
            var serverTimeUtc = DateTimeOffset.UtcNow;

            var products = await FetchAllProductsAsync(warehouseId, since, ct);
            var discounts = await discountService.ListAsync(activeOnly: true, updatedSince: since, ct: ct);
            var employees = await employeeService.ListAsync(ct);
            var expenseCategories = await expenseCategoryService.ListAsync(ct);
            var customers = await FetchAllCustomersAsync(since, ct);
            var taxonomy = await taxonomyService.GetSnapshotAsync(ct);
            var posSettings = await posSettingsService.GetAsync(ct);
            var barcodeSettings = await barcodeService.GetSettingsAsync(ct);
            var currencySettings = await currencySettingsService.GetAsync(ct);

            db.SyncLogs.Add(new SyncLog
            {
                TerminalId = currentUser.TerminalId,
                Direction = SyncDirection.Pull,
                EntityType = entityType,
                Status = SyncLogStatus.Success,
            });
            await db.SaveChangesAsync(ct);

            return new SyncSnapshotDto(
                serverTimeUtc, products, discounts, employees, expenseCategories, customers,
                taxonomy, posSettings, barcodeSettings, currencySettings);
        }
        catch (Exception ex)
        {
            db.SyncLogs.Add(new SyncLog
            {
                TerminalId = currentUser.TerminalId,
                Direction = SyncDirection.Pull,
                EntityType = entityType,
                Status = SyncLogStatus.Failed,
                ErrorMessage = ex.Message,
            });
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    /// <summary>ProductService caps PageSize at 200 (see PagedQuery), so a catalog bigger than
    /// that needs multiple pages stitched together — the client still gets one flat list.</summary>
    private async Task<List<ProductDto>> FetchAllProductsAsync(Guid warehouseId, DateTimeOffset? since, CancellationToken ct)
    {
        var results = new List<ProductDto>();
        // Active-only mirrors the online POS's own product search (pos.component.ts), so an
        // offline terminal never offers a discontinued/inactive product for sale that the online
        // flow would already hide.
        var query = new ProductListQuery { WarehouseId = warehouseId, PageSize = 200, UpdatedSince = since, Status = "Active" };
        while (true)
        {
            var page = await productService.ListAsync(query, ct);
            results.AddRange(page.Items);
            if (page.Items.Count == 0 || results.Count >= page.TotalCount) break;
            query.Page++;
        }
        return results;
    }

    private async Task<List<CustomerDto>> FetchAllCustomersAsync(DateTimeOffset? since, CancellationToken ct)
    {
        var results = new List<CustomerDto>();
        var query = new CustomerListQuery { PageSize = 200, UpdatedSince = since };
        while (true)
        {
            var page = await customerService.ListAsync(query, ct);
            results.AddRange(page.Items);
            if (page.Items.Count == 0 || results.Count >= page.TotalCount) break;
            query.Page++;
        }
        return results;
    }

    /// <summary>Reuses SalesService's own Query()/ToDto (widened to internal for exactly this
    /// purpose) rather than re-implementing the Include chain and mapping — the only new work
    /// here is the ReturnPolicyDays window and attaching each line's ReturnedQuantity, computed
    /// the same way ReturnsService.LookupAsync already does it for the online Returns screen.
    /// ReturnedQuantity can go stale on a delta pull for an order that was only partially
    /// returned after that order's own row was last synced (a ReturnLine insert doesn't touch
    /// Order.UpdatedAtUtc) — acceptable for now since offline Returns (Phase 2c) only targets
    /// already-synced orders and a subsequent full resync corrects it; documented gap, not a bug.</summary>
    public async Task<IReadOnlyList<OrderSyncDto>> PullOrdersAsync(DateTimeOffset? since, CancellationToken ct = default)
    {
        var warehouseId = currentUser.ResolveWarehouseScope((Guid?)null)
            ?? throw new ConflictException("A POS terminal must be selected to synchronize.");

        var posSettings = await posSettingsService.GetAsync(ct);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, posSettings.ReturnPolicyDays));

        var query = SalesService.Query(db).Where(o => o.WarehouseId == warehouseId && o.CreatedAtUtc >= cutoff);
        if (since is { } s)
        {
            query = query.Where(o => o.CreatedAtUtc > s || (o.UpdatedAtUtc != null && o.UpdatedAtUtc > s));
        }

        var orders = await query.OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);
        var cashierNames = await SalesService.GetCashierNamesAsync(db, orders, ct);

        var lineIds = orders.SelectMany(o => o.Lines.Select(l => l.Id)).ToList();
        var returnedByLine = await db.ReturnLines
            .Where(rl => lineIds.Contains(rl.OrderLineId))
            .GroupBy(rl => rl.OrderLineId)
            .Select(g => new { OrderLineId = g.Key, Quantity = g.Sum(rl => rl.Quantity) })
            .ToDictionaryAsync(x => x.OrderLineId, x => x.Quantity, ct);

        return orders.Select(o =>
        {
            var dto = SalesService.ToDto(o, cashierNames);
            var lines = dto.Lines
                .Select(l => new OrderSyncLineDto(
                    l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.TaxRatePercent, l.DiscountPercent, l.LineTotal,
                    returnedByLine.GetValueOrDefault(l.Id)))
                .ToList();
            return new OrderSyncDto(
                dto.Id, dto.OrderNumber, dto.CustomerId, dto.CustomerName, dto.WarehouseId, dto.WarehouseName,
                dto.Channel, dto.Status, dto.Subtotal, dto.DiscountAmount, dto.TaxAmount, dto.Total, dto.DiscountLabel,
                dto.SalesPersonId, dto.SalesPersonName, dto.PaymentMethod, dto.Notes, dto.CashierName, dto.Store,
                lines, dto.CreatedAtUtc, dto.ClientTransactionId, dto.CapturedOffline);
        }).ToList();
    }

    public async Task<PagedResult<SyncLogDto>> ListLogsAsync(SyncLogListQuery query, CancellationToken ct = default)
    {
        var logs = db.SyncLogs.AsQueryable();

        // Terminal-scoped for a Cashier token (pinned to its own terminal via ResolveTerminalScope);
        // unrestricted for SuperAdmin/HeadOffice/StoreManager, matching every other admin-visible-
        // but-terminal-scoped endpoint already built (Sales Detail report's Terminal filter, etc.).
        if (currentUser.ResolveTerminalScope(query.TerminalId) is { } tid) logs = logs.Where(l => l.TerminalId == tid);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<SyncLogStatus>(query.Status, true, out var status))
        {
            logs = logs.Where(l => l.Status == status);
        }

        var totalCount = await logs.CountAsync(ct);
        var page = await logs
            .OrderByDescending(l => l.OccurredAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var terminalIds = page.Where(l => l.TerminalId.HasValue).Select(l => l.TerminalId!.Value).Distinct().ToList();
        var terminalNames = await db.PosTerminals
            .Where(t => terminalIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var items = page.Select(l => new SyncLogDto(
            l.Id, l.TerminalId,
            l.TerminalId.HasValue && terminalNames.TryGetValue(l.TerminalId.Value, out var name) ? name : null,
            l.Direction.ToString(), l.EntityType, l.EntityId, l.ClientTransactionId,
            l.Status.ToString(), l.ErrorMessage, l.OccurredAtUtc)).ToList();

        return new PagedResult<SyncLogDto> { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }
}
