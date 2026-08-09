using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Inventory;

public class Warehouse : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? City { get; set; }
    public PartyStatus Status { get; set; } = PartyStatus.Active;

    public Guid? StoreId { get; set; }
    public Store? Store { get; set; }
}
