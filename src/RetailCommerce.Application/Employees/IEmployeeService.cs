using RetailCommerce.Application.TaxonomyAdmin;

namespace RetailCommerce.Application.Employees;

/// <summary>Lightweight roster of floor staff who can be recorded as the "sales person" on a
/// sale, for commission/accountability — no login required. Reuses the generic Code/Name/
/// IsActive DTOs already built for the taxonomy admin lists.</summary>
public interface IEmployeeService
{
    Task<IReadOnlyList<TaxonomyItemDto>> ListAsync(CancellationToken ct = default);
    Task<TaxonomyItemDto> CreateAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task<TaxonomyItemDto> UpdateAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
