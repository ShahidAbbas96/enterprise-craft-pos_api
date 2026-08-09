using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Warehouses;

public interface IWarehouseService
{
    Task<PagedResult<WarehouseDto>> ListAsync(WarehouseListQuery query, CancellationToken ct = default);
    Task<WarehouseDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<WarehouseDto> CreateAsync(UpsertWarehouseRequest request, CancellationToken ct = default);
    Task<WarehouseDto> UpdateAsync(Guid id, UpsertWarehouseRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
