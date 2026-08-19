using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Sales;

public record SaleLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent,
    decimal DiscountPercent,
    decimal LineTotal);

/// <summary>Store contact/tax-registration details for the receipt header/footer — purely
/// informational (this system has no FBR e-invoicing integration), null when the selling
/// warehouse has no associated Store or the Store hasn't filled these in.</summary>
public record ReceiptStoreInfoDto(
    string? Address,
    string? Phone,
    string? Email,
    string? Ntn,
    string? Strn,
    string? FooterText);

public record SaleDto(
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
    IReadOnlyList<SaleLineDto> Lines,
    DateTimeOffset CreatedAtUtc);

public record CreateSaleLineInput(Guid ProductId, int Quantity);

public record CreateSaleRequest(
    Guid? CustomerId,
    /// <summary>Omittable for a terminal-scoped POS caller (filled from its token via
    /// ICurrentUserService.ResolveWarehouseScope) — required for a back-office caller, which
    /// gets a ConflictException if it's left null.</summary>
    Guid? WarehouseId,
    string PaymentMethod,
    decimal DiscountPercent,
    string? DiscountLabel,
    Guid? SalesPersonId,
    string? Notes,
    IReadOnlyList<CreateSaleLineInput> Lines);

public class SaleListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
}
