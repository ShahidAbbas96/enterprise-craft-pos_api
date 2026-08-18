using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> ListAsync(ProductListQuery query, CancellationToken ct = default);
    Task<ProductDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(UpsertProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpsertProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ProductFieldConfigDto>> GetFieldConfigAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductFieldConfigDto>> UpdateFieldConfigAsync(IReadOnlyList<UpdateProductFieldConfigRequest> requests, CancellationToken ct = default);
}
