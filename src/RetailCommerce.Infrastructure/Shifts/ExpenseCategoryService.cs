using RetailCommerce.Application.Shifts;
using RetailCommerce.Application.TaxonomyAdmin;
using RetailCommerce.Domain.Shifts;
using RetailCommerce.Infrastructure.Persistence;
using RetailCommerce.Infrastructure.TaxonomyAdmin;

namespace RetailCommerce.Infrastructure.Shifts;

public class ExpenseCategoryService(AppDbContext db) : IExpenseCategoryService
{
    private readonly SimpleTaxonomyNodeService<ExpenseCategory> _inner = new(db);

    public Task<IReadOnlyList<TaxonomyItemDto>> ListAsync(CancellationToken ct = default) => _inner.ListAsync(ct);
    public Task<TaxonomyItemDto> CreateAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _inner.CreateAsync(request, ct);
    public Task<TaxonomyItemDto> UpdateAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _inner.UpdateAsync(id, request, ct);
    public Task DeleteAsync(Guid id, CancellationToken ct = default) => _inner.DeleteAsync(id, ct);
}
