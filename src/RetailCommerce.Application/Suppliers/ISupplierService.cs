using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Suppliers;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> ListAsync(SupplierListQuery query, CancellationToken ct = default);
    Task<SupplierDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<SupplierDto> CreateAsync(UpsertSupplierRequest request, CancellationToken ct = default);
    Task<SupplierDto> UpdateAsync(Guid id, UpsertSupplierRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
