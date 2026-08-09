using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Stores;

public interface IStoreService
{
    Task<PagedResult<StoreDto>> ListAsync(StoreListQuery query, CancellationToken ct = default);
    Task<StoreDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<StoreDto> CreateAsync(UpsertStoreRequest request, CancellationToken ct = default);
    Task<StoreDto> UpdateAsync(Guid id, UpsertStoreRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
