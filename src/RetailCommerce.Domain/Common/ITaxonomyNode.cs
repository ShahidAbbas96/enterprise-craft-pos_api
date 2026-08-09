namespace RetailCommerce.Domain.Common;

/// <summary>Implemented by the flat, parent-less taxonomy lookups (Department, Gender,
/// EventType) so a single generic admin service can manage all three instead of three
/// near-duplicate implementations.</summary>
public interface ITaxonomyNode
{
    Guid Id { get; set; }
    string Code { get; set; }
    string Name { get; set; }
    bool IsActive { get; set; }
}
