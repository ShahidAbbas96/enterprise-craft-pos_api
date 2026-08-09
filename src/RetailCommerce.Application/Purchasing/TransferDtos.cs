using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Purchasing;

public record TransferLineDto(Guid ProductId, string Sku, string ProductName, int Quantity, string Unit);

public record TransferDto(
    Guid Id,
    string TransferNumber,
    Guid FromWarehouseId,
    string FromWarehouseName,
    Guid ToWarehouseId,
    string ToWarehouseName,
    DateOnly TransferDate,
    string? Reference,
    string? Notes,
    string Status,
    IReadOnlyList<TransferLineDto> Lines,
    DateTimeOffset CreatedAtUtc);

public record CreateTransferLineInput(Guid ProductId, int Quantity, string Unit);

public record CreateTransferRequest(
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    DateOnly TransferDate,
    string? Reference,
    string? Notes,
    IReadOnlyList<CreateTransferLineInput> Lines);

public class TransferListQuery : PagedQuery
{
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string? Status { get; set; }
}
