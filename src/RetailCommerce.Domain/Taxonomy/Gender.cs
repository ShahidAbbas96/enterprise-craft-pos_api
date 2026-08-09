using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>Target segment, e.g. WOMEN, KIDS, MEN, UNISEX. Kept as a managed lookup, not an enum,
/// because the client's full list is not yet confirmed (see CLAUDE.md open questions).</summary>
public class Gender : BaseEntity, ITaxonomyNode
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
