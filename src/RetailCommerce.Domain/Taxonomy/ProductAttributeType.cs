using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>A department-aware, configurable attribute dimension, e.g. "UPPER_MATERIAL", "HEEL_HEIGHT".
/// When DepartmentId is null the attribute applies across all departments; otherwise it only
/// applies (and should only be shown/edited in the UI) for that one department. This is the
/// EAV-style design chosen so a new department can define its own attribute set without a
/// schema migration (see CLAUDE.md §5.2).</summary>
public class ProductAttributeType : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<ProductAttributeOption> Options { get; set; } = new List<ProductAttributeOption>();
}
