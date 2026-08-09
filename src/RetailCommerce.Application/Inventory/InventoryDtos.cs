using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Inventory;

public record InventoryBalanceDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    string? ItemCode,
    string? Barcode,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int MinStock,
    int ReorderLevel,
    bool IsLowStock);

public class InventoryListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? ProductId { get; set; }
    public bool LowStockOnly { get; set; }
}

public record AdjustStockRequest(Guid ProductId, Guid WarehouseId, int CountedQuantity, string? Reason);

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    int QuantityDelta,
    string Kind,
    string? Reference,
    DateTimeOffset CreatedAtUtc);

public class StockMovementListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Kind { get; set; }
}
