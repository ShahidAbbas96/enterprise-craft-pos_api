using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>Top-level line of business, e.g. FOOTWEAR, BAGS, JEWELLERY. Client-managed, not hardcoded.</summary>
public class Department : BaseEntity, ITaxonomyNode
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<ProductAttributeType> AttributeTypes { get; set; } = new List<ProductAttributeType>();
}
