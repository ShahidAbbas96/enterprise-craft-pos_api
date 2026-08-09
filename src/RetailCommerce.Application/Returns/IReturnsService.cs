using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Returns;

/// <summary>Product returns against a past invoice — restocks inventory and records what came
/// back, without tracking a refund payment method (just what/how much, per the client's
/// requirements). Partial returns are supported; the original order only flips to Refunded
/// once every line has been fully returned.</summary>
public interface IReturnsService
{
    Task<ReturnableSaleDto> LookupAsync(string orderNumber, CancellationToken ct = default);
    Task<ReturnDto> CreateReturnAsync(CreateReturnRequest request, Guid? userId, CancellationToken ct = default);
    Task<PagedResult<ReturnDto>> ListAsync(ReturnListQuery query, CancellationToken ct = default);
}
