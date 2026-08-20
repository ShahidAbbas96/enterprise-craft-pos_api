using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Customers;
using RetailCommerce.Application.Discounts;
using RetailCommerce.Application.Employees;
using RetailCommerce.Application.Products;
using RetailCommerce.Application.Settings;
using RetailCommerce.Application.Shifts;
using RetailCommerce.Application.Sync;
using RetailCommerce.Application.Taxonomy;
using RetailCommerce.Domain.Sync;
using RetailCommerce.Infrastructure.Persistence;

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
