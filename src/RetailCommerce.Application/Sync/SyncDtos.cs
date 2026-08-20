using RetailCommerce.Application.Common;
using RetailCommerce.Application.Customers;
using RetailCommerce.Application.Discounts;
using RetailCommerce.Application.Products;
using RetailCommerce.Application.Settings;
using RetailCommerce.Application.Taxonomy;
using RetailCommerce.Application.TaxonomyAdmin;

namespace RetailCommerce.Application.Sync;

/// <summary>Same shape for both a full bootstrap (SyncService.BootstrapAsync — ignores any
/// cursor) and a delta pull (SyncService.PullAsync?since=... — Products/Discounts/Customers
/// filtered to rows created/updated after the cursor; Employees/ExpenseCategories/Taxonomy/
/// settings are small enough to always send in full either way). ServerTimeUtc is the value the
/// client must persist as its new cursor for the next pull — using the server's own clock (not
/// the client's) avoids clock-skew bugs.</summary>
public record SyncSnapshotDto(
    DateTimeOffset ServerTimeUtc,
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<DiscountDto> Discounts,
    IReadOnlyList<TaxonomyItemDto> Employees,
    IReadOnlyList<TaxonomyItemDto> ExpenseCategories,
    IReadOnlyList<CustomerDto> Customers,
    TaxonomySnapshotDto Taxonomy,
    PosSettingsDto PosSettings,
    BarcodeSettingsDto BarcodeSettings,
    CurrencySettingsDto CurrencySettings);

public record SyncLogDto(
    Guid Id,
    Guid? TerminalId,
    string? TerminalName,
    string Direction,
    string EntityType,
    Guid? EntityId,
    Guid? ClientTransactionId,
    string Status,
    string? ErrorMessage,
    DateTimeOffset OccurredAtUtc);

public class SyncLogListQuery : PagedQuery
{
    public Guid? TerminalId { get; set; }
    public string? Status { get; set; }
}
