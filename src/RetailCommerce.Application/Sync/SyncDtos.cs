using RetailCommerce.Application.Common;
using RetailCommerce.Application.Customers;
using RetailCommerce.Application.Discounts;
using RetailCommerce.Application.Products;
using RetailCommerce.Application.Sales;
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

/// <summary>SaleLineDto plus how much of that line has already been returned — carried
/// separately from SaleLineDto itself (rather than adding ReturnedQuantity there) since every
/// other Sale endpoint has no use for it and would otherwise pay for a ReturnLines query it never
/// needs.</summary>
public record OrderSyncLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent,
    decimal DiscountPercent,
    decimal LineTotal,
    int ReturnedQuantity);

/// <summary>Feeds an offline POS terminal's local "recent orders" cache (GET /api/sync/orders) —
/// same shape as SaleDto, just with OrderSyncLineDto lines so Search Slip/POS Reports/Returns can
/// all work from this one cached record without a live call. Scoped to the caller's own resolved
/// warehouse and windowed to roughly PosSettings.ReturnPolicyDays, matching how long a sale stays
/// return-eligible anyway.</summary>
public record OrderSyncDto(
    Guid Id,
    string OrderNumber,
    Guid? CustomerId,
    string? CustomerName,
    Guid WarehouseId,
    string WarehouseName,
    string Channel,
    string Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal Total,
    string? DiscountLabel,
    Guid? SalesPersonId,
    string? SalesPersonName,
    string PaymentMethod,
    string? Notes,
    string? CashierName,
    ReceiptStoreInfoDto? Store,
    IReadOnlyList<OrderSyncLineDto> Lines,
    DateTimeOffset CreatedAtUtc,
    Guid? ClientTransactionId,
    bool CapturedOffline);

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
