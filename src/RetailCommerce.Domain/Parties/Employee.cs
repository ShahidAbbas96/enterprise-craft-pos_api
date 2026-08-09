using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Parties;

/// <summary>Lightweight floor-staff roster used to record who sold an item (commission/
/// accountability), separate from ApplicationUser login accounts — most sales associates in a
/// retail store don't need system logins.</summary>
public class Employee : BaseEntity, ITaxonomyNode
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
