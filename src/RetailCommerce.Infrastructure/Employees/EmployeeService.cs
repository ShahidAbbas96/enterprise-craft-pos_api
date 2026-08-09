using RetailCommerce.Application.Employees;
using RetailCommerce.Application.TaxonomyAdmin;
using RetailCommerce.Domain.Parties;
using RetailCommerce.Infrastructure.Persistence;
using RetailCommerce.Infrastructure.TaxonomyAdmin;

namespace RetailCommerce.Infrastructure.Employees;

public class EmployeeService(AppDbContext db) : IEmployeeService
{
    private readonly SimpleTaxonomyNodeService<Employee> _inner = new(db);

    public Task<IReadOnlyList<TaxonomyItemDto>> ListAsync(CancellationToken ct = default) => _inner.ListAsync(ct);
    public Task<TaxonomyItemDto> CreateAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _inner.CreateAsync(request, ct);
    public Task<TaxonomyItemDto> UpdateAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _inner.UpdateAsync(id, request, ct);
    public Task DeleteAsync(Guid id, CancellationToken ct = default) => _inner.DeleteAsync(id, ct);
}
