using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Warehouses;

public record WarehouseDto(Guid Id, string Code, string Name, string? City, string Status, Guid? StoreId, string? StoreName);

public record UpsertWarehouseRequest(string Code, string Name, string? City, string Status, Guid? StoreId);

public class WarehouseListQuery : PagedQuery
{
    public Guid? StoreId { get; set; }
}
