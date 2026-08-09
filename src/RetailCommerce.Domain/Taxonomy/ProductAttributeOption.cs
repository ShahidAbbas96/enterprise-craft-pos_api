using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>An allowed value for a given attribute type, e.g. "METAL"/"RAW SILK" for UPPER_MATERIAL.</summary>
public class ProductAttributeOption : BaseEntity
{
    public Guid ProductAttributeTypeId { get; set; }
    public ProductAttributeType ProductAttributeType { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    /// <summary>Short (1-3 char) segment used when building a product barcode, e.g. "BK" for
    /// BLACK. Optional admin override — when null, the barcode generator auto-derives one from
    /// Name, which can collide for similarly-named options (BLACK/BLUE/BROWN all start "BL").</summary>
    public string? BarcodeCode { get; set; }
}
