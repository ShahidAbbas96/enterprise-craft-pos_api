using RetailCommerce.Domain.Common;

namespace RetailCommerce.Application.Products;

/// <summary>The fixed set of Product fields an admin can configure as Required/Optional/Hidden
/// (Settings → Product Fields). Deliberately excludes Name, Sku, ItemCode, Barcode, Cost, Price,
/// Unit and Status — those 8 are the "core" fields from the feature spec and stay unconditionally
/// required (Sku/ItemCode/Barcode are also system-generated, never user-editable at all). Both
/// ProductService (validation/read) and DbSeeder (seeding the rows once) share this single list so
/// the two can never drift out of sync.</summary>
public static class ProductFieldCatalog
{
    public static readonly IReadOnlyList<(string Key, string DisplayName, ProductFieldState Default)> Fields =
    [
        ("Department", "Department", ProductFieldState.Required),
        ("Gender", "Gender", ProductFieldState.Required),
        ("EventType", "Event Type", ProductFieldState.Required),
        ("Category", "Category", ProductFieldState.Required),
        ("Subcategory", "Subcategory", ProductFieldState.Optional),
        ("Collection", "Collection", ProductFieldState.Optional),
        ("Year", "Year", ProductFieldState.Optional),
        ("Supplier", "Supplier", ProductFieldState.Optional),
        ("WholesalePrice", "Wholesale Price", ProductFieldState.Optional),
        ("TaxRatePercent", "Tax Rate %", ProductFieldState.Optional),
        ("DiscountPercent", "Discount %", ProductFieldState.Optional),
        ("MinStock", "Min Stock", ProductFieldState.Optional),
        ("MaxStock", "Max Stock", ProductFieldState.Optional),
        ("ReorderLevel", "Reorder Level", ProductFieldState.Optional),
        ("Location", "Location", ProductFieldState.Optional),
        ("Description", "Description", ProductFieldState.Optional),
        ("ImageUrl", "Image URL", ProductFieldState.Optional),
    ];
}
