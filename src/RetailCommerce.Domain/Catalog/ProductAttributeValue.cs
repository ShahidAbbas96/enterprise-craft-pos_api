using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Taxonomy;

namespace RetailCommerce.Domain.Catalog;

/// <summary>Join row assigning one attribute option (e.g. Upper Material = "RAW SILK") to a
/// product. A product has zero or more of these depending on which attribute types apply to
/// its department (see ProductAttributeType.DepartmentId).</summary>
public class ProductAttributeValue : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid ProductAttributeTypeId { get; set; }
    public ProductAttributeType ProductAttributeType { get; set; } = default!;

    public Guid ProductAttributeOptionId { get; set; }
    public ProductAttributeOption ProductAttributeOption { get; set; } = default!;
}
