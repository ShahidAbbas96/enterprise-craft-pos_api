using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Inventory;

public interface IInventoryService
{
    Task<PagedResult<InventoryBalanceDto>> ListAsync(InventoryListQuery query, CancellationToken ct = default);

    /// <summary>Reconciles the system quantity to a physically counted quantity — the standard
    /// cycle-count workflow. Always writes a StockMovement (Kind=Adjustment) explaining the
    /// delta; never silently overwrites a balance.</summary>
    Task<InventoryBalanceDto> AdjustAsync(AdjustStockRequest request, Guid? userId, CancellationToken ct = default);

    Task<PagedResult<StockMovementDto>> ListMovementsAsync(StockMovementListQuery query, CancellationToken ct = default);
}
