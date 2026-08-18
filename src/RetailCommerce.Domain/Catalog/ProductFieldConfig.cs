using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Catalog;

/// <summary>Admin-controlled Required/Optional/Hidden state for one configurable Product field
/// (Settings → Product Fields). One row per FieldKey, seeded once for every known key by
/// DbSeeder — rows are never created/removed through the API, only their State is updated.
/// Sku and ItemCode are deliberately NOT configurable here: they're always shown, always
/// required, and always system-generated/read-only (see ProductService's code generation).</summary>
public class ProductFieldConfig : BaseEntity
{
    public string FieldKey { get; set; } = default!;
    public ProductFieldState State { get; set; } = ProductFieldState.Optional;
}
