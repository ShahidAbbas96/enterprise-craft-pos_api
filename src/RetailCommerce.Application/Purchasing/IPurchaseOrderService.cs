using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Purchasing;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken ct = default);
    Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, Guid? userId, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);

    /// <summary>The only action that increases inventory — mirrors receive_purchase_order
    /// from the reference prototype's SQL, reimplemented as a C# transaction.</summary>
    Task<PurchaseOrderDto> ReceiveAsync(Guid id, Guid? userId, CancellationToken ct = default);
}
