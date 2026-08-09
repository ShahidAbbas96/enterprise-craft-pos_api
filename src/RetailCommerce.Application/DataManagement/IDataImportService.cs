namespace RetailCommerce.Application.DataManagement;

/// <summary>Bulk-loads Products or Inventory counts from an uploaded .xlsx. Each row is processed
/// independently (upsert-by-Sku for products, adjust-by-Sku+WarehouseCode for inventory) so one
/// bad row doesn't abort the whole file — every row's outcome is reported back.</summary>
public interface IDataImportService
{
    Task<ProductImportResultDto> ImportProductsAsync(Stream fileStream, CancellationToken ct = default);

    Task<InventoryImportResultDto> ImportInventoryAsync(Stream fileStream, CancellationToken ct = default);
}
