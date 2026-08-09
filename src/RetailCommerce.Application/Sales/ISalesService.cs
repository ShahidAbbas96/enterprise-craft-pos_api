using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Sales;

public interface ISalesService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, Guid? cashierUserId, CancellationToken ct = default);
    Task<SaleDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SaleDto>> ListAsync(SaleListQuery query, CancellationToken ct = default);
}
