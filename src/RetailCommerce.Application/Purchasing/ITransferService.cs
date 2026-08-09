using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Purchasing;

public interface ITransferService
{
    Task<PagedResult<TransferDto>> ListAsync(TransferListQuery query, CancellationToken ct = default);
    Task<TransferDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<TransferDto> CreateAsync(CreateTransferRequest request, Guid? userId, CancellationToken ct = default);

    /// <summary>The only action that moves inventory — mirrors complete_transfer from the
    /// reference prototype's SQL, reimplemented as a C# transaction.</summary>
    Task<TransferDto> CompleteAsync(Guid id, Guid? userId, CancellationToken ct = default);

    Task<TransferDto> CancelAsync(Guid id, CancellationToken ct = default);
}
