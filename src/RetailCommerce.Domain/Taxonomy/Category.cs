using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>Product type within a department, e.g. KHUSSA/SANDAL under FOOTWEAR.</summary>
public class Category : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
