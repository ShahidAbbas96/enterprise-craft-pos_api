using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Stores;

public record StoreDto(
    Guid Id,
    string Code,
    string Name,
    string? City,
    string? Address,
    string? Phone,
    string? Email,
    string? Ntn,
    string? Strn,
    string? ReceiptFooterText,
    string Status,
    int WarehouseCount);

public record UpsertStoreRequest(
    string Code,
    string Name,
    string? City,
    string? Address,
    string? Phone,
    string? Email,
    string? Ntn,
    string? Strn,
    string? ReceiptFooterText,
    string Status);

public class StoreListQuery : PagedQuery
{
}
