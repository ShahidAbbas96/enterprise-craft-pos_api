using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken ct = default);
    Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(UpsertCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpsertCustomerRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
