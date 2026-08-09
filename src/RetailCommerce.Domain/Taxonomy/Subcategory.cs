using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>Refinement within a category, e.g. REGULAR/PESHAWARI KHUSSA under KHUSSA. "Default" is a
/// valid value for categories with no meaningful subcategory (matches the client's source data).</summary>
public class Subcategory : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
