namespace RetailCommerce.Application.DataManagement;

/// <summary>Bulk-loads Products or Inventory counts from an uploaded .xlsx. Each row is processed
/// independently (upsert-by-Sku for products, adjust-by-Sku+WarehouseCode for inventory) so one
/// bad row doesn't abort the whole file — every row's outcome is reported back.</summary>
public interface IDataImportService
{
    Task<ProductImportResultDto> ImportProductsAsync(Stream fileStream, CancellationToken ct = default);

    Task<InventoryImportResultDto> ImportInventoryAsync(Stream fileStream, CancellationToken ct = default);

    /// <summary>Bulk-loads per-product discounts from a D365-style CSV export (columns: itemId,
    /// ColorID, SizeID, iDiscountMethod, dDiscountPercentage, dcashDiscount, dDiscountPrice —
    /// ColorID/SizeID are accepted but unused, since each product row here already represents one
    /// specific size/color variant). iDiscountMethod selects which value column is read: "Discount
    /// percentage" -> dDiscountPercentage, "Cash discount amount" -> dcashDiscount, "Discount
    /// price" -> dDiscountPrice (the exact final per-unit selling price, stored as-is — see
    /// DiscountValueType.FixedPrice). Any discount already active on a matched product is
    /// deactivated before the imported one is added, so a product never carries two active
    /// discounts at once.</summary>
    Task<DiscountImportResultDto> ImportDiscountsAsync(Stream fileStream, CancellationToken ct = default);

    /// <summary>The Product Import Template as .xlsx bytes — column set/order is data-driven:
    /// the 8 core columns always come first (Name, SKU, ItemCode, Barcode, Status, Cost, Price,
    /// Unit), then every ProductFieldConfig field currently set to Required/Optional (Hidden ones
    /// are omitted), then one column per active ProductAttributeType.</summary>
    Task<byte[]> BuildProductTemplateAsync(CancellationToken ct = default);
}
