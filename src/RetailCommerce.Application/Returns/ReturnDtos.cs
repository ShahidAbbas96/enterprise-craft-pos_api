using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Returns;

public record ReturnableLineDto(
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    int QuantitySold,
    int QuantityAlreadyReturned,
    int QuantityReturnable,
    decimal UnitPrice);

public record ReturnableSaleDto(
    Guid OrderId,
    string OrderNumber,
    Guid WarehouseId,
    string WarehouseName,
    string? CustomerName,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ReturnableLineDto> Lines);

public record CreateReturnLineInput(Guid OrderLineId, int Quantity);

public record CreateReturnRequest(Guid OrderId, string? Reason, IReadOnlyList<CreateReturnLineInput> Lines);

public record ReturnLineDto(Guid OrderLineId, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record ReturnDto(
    Guid Id,
    string ReturnNumber,
    Guid OrderId,
    string OrderNumber,
    Guid WarehouseId,
    string WarehouseName,
    string? CustomerName,
    string? Reason,
    decimal Total,
    string? CreatedByName,
    IReadOnlyList<ReturnLineDto> Lines,
    DateTimeOffset CreatedAtUtc);

public class ReturnListQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
}
