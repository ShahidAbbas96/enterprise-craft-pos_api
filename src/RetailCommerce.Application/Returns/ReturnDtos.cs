using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Returns;

public record ReturnableLineDto(
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    int QuantitySold,
    int QuantityAlreadyReturned,
    int QuantityReturnable,
    decimal UnitPrice,
    /// <summary>The original sale line's own DiscountPercent — needed client-side so an offline
    /// POS can compute the exact refund (lineSubtotal - round(lineSubtotal * DiscountPercent/100,
    /// 2), mirroring CreateReturnAsync's own formula) for its provisional receipt before the real
    /// return has synced.</summary>
    decimal DiscountPercent);

public record ReturnableSaleDto(
    Guid OrderId,
    string OrderNumber,
    Guid WarehouseId,
    string WarehouseName,
    string? CustomerName,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ReturnableLineDto> Lines);

public record CreateReturnLineInput(Guid OrderLineId, int Quantity);

public record CreateReturnRequest(
    Guid OrderId,
    string? Reason,
    IReadOnlyList<CreateReturnLineInput> Lines,
    /// <summary>Client-generated (crypto.randomUUID()) idempotency key from the POS offline
    /// outbox — same pattern as CreateSaleRequest.ClientTransactionId. Null for a return created
    /// directly online (today's live Returns screen has no outbox), which skips idempotency.</summary>
    Guid? ClientTransactionId = null);

public record ReturnLineDto(Guid OrderLineId, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record ReturnDto(
    Guid Id,
    string ReturnNumber,
    Guid OrderId,
    string OrderNumber,
    Guid WarehouseId,
    string WarehouseName,
    string? CustomerName,
    string? Reason,
    decimal Total,
    string? CreatedByName,
    IReadOnlyList<ReturnLineDto> Lines,
    DateTimeOffset CreatedAtUtc,
    Guid? ClientTransactionId);

public class ReturnListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
}
