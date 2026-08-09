using RetailCommerce.Application.TaxonomyAdmin;

namespace RetailCommerce.Application.Shifts;

/// <summary>Settings-managed list of expense categories cashiers pick from when logging an
/// expense during shift close. Reuses the generic Code/Name/IsActive DTOs.</summary>
public interface IExpenseCategoryService
{
    Task<IReadOnlyList<TaxonomyItemDto>> ListAsync(CancellationToken ct = default);
    Task<TaxonomyItemDto> CreateAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task<TaxonomyItemDto> UpdateAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
