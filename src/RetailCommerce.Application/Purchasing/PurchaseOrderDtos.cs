using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Purchasing;

public record PurchaseOrderLineDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitCost,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal);

public record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    Guid WarehouseId,
    string WarehouseName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string? Reference,
    string? Notes,
    string Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTimeOffset CreatedAtUtc);

public record CreatePurchaseOrderLineInput(Guid ProductId, int Quantity, decimal UnitCost, decimal DiscountPercent, decimal TaxPercent);

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    Guid WarehouseId,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string? Reference,
    string? Notes,
    bool Submit,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines);

public record UpdatePurchaseOrderStatusRequest(string Status);

public class PurchaseOrderListQuery : PagedQuery
{
    public Guid? SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Status { get; set; }
}
