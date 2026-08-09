using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>Occasion/usage tier, e.g. BASIC, CASUAL, FORMAL, COTURE.</summary>
public class EventType : BaseEntity, ITaxonomyNode
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
