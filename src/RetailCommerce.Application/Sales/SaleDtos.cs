using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Sales;

public record SaleLineDto(
    /// <summary>The real server-assigned OrderLine.Id — added so an offline POS's local "recent
    /// orders" cache (fed by GET /api/sync/orders) can reference a specific line for a Return
    /// without a second round trip, exactly like ReturnableLineDto already does for the online
    /// Returns screen.</summary>
    Guid Id,
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
    DateTimeOffset CreatedAtUtc,
    /// <summary>Echoes the request's idempotency key (when it carried one) so an offline POS's
    /// sync engine can positively match this response back to the outbox row that sent it.</summary>
    Guid? ClientTransactionId,
    bool CapturedOffline);

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
    IReadOnlyList<CreateSaleLineInput> Lines,
    /// <summary>Client-generated (crypto.randomUUID()) idempotency key from the POS offline
    /// outbox. Null for callers that never go through the outbox (e.g. today's online-only admin
    /// tooling, if any exists) — CreateSaleAsync only enforces idempotency when this is set.</summary>
    Guid? ClientTransactionId = null,
    /// <summary>True when the POS was offline at the moment this sale was rung up (set by the
    /// client's ConnectivityService at enqueue time, not at retry/drain time) — relaxes the
    /// stock check to allow negative inventory instead of rejecting a sale that already
    /// physically happened. Defaults to false so every other caller keeps today's strict
    /// behavior unchanged.</summary>
    bool CapturedOffline = false);

public class SaleListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
}
