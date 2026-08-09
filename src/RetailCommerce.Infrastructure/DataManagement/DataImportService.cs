using ClosedXML.Excel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.DataManagement;
using RetailCommerce.Application.Inventory;
using RetailCommerce.Application.Products;
using RetailCommerce.Domain.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.DataManagement;

public class DataImportService(
    AppDbContext db,
    IProductService productService,
    IInventoryService inventoryService,
    IBarcodeService barcodeService,
    IValidator<UpsertProductRequest> productValidator) : IDataImportService
{
    public async Task<ProductImportResultDto> ImportProductsAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheets.First();
        var columns = ReadHeaderRow(ws);

        var departments = await db.Departments.ToListAsync(ct);
        var genders = await db.Genders.ToListAsync(ct);
        var eventTypes = await db.EventTypes.ToListAsync(ct);
        var categories = await db.Categories.ToListAsync(ct);
        var subcategories = await db.Subcategories.ToListAsync(ct);
        var suppliers = await db.Suppliers.ToListAsync(ct);
        var products = await db.Products.Select(p => new { p.Id, p.Sku }).ToListAsync(ct);
        var productIdBySku = products.ToDictionary(p => p.Sku, p => p.Id, StringComparer.OrdinalIgnoreCase);

        var attributeTypes = await db.ProductAttributeTypes.ToListAsync(ct);
        var attributeOptions = await db.ProductAttributeOptions.ToListAsync(ct);
        var attributeTypeByCode = attributeTypes.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        var attributeColumnHeaders = columns.Keys.Where(h => attributeTypeByCode.ContainsKey(h)).ToList();

        var errors = new List<ImportRowError>();
        int created = 0, updated = 0, failed = 0, totalRows = 0;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            totalRows++;
            var rowNum = row.RowNumber();
            var sku = GetString(row, columns, "SKU");

            try
            {
                if (string.IsNullOrWhiteSpace(sku)) throw new InvalidOperationException("SKU is required.");

                var departmentCode = GetString(row, columns, "Department") ?? throw new InvalidOperationException("Department is required.");
                var department = departments.FirstOrDefault(d => d.Code.Equals(departmentCode, StringComparison.OrdinalIgnoreCase))
                                  ?? throw new InvalidOperationException($"Unknown department code '{departmentCode}'.");

                var categoryCode = GetString(row, columns, "Category") ?? throw new InvalidOperationException("Category is required.");
                var category = categories.FirstOrDefault(c => c.DepartmentId == department.Id && c.Code.Equals(categoryCode, StringComparison.OrdinalIgnoreCase))
                                ?? throw new InvalidOperationException($"Unknown category code '{categoryCode}' for department '{departmentCode}'.");

                Guid? subcategoryId = null;
                var subcategoryCode = GetString(row, columns, "Subcategory");
                if (!string.IsNullOrWhiteSpace(subcategoryCode))
                {
                    var subcategory = subcategories.FirstOrDefault(s => s.CategoryId == category.Id && s.Code.Equals(subcategoryCode, StringComparison.OrdinalIgnoreCase))
                                       ?? throw new InvalidOperationException($"Unknown subcategory code '{subcategoryCode}' for category '{categoryCode}'.");
                    subcategoryId = subcategory.Id;
                }

                var genderCode = GetString(row, columns, "Gender") ?? throw new InvalidOperationException("Gender is required.");
                var gender = genders.FirstOrDefault(g => g.Code.Equals(genderCode, StringComparison.OrdinalIgnoreCase))
                             ?? throw new InvalidOperationException($"Unknown gender code '{genderCode}'.");

                var eventCode = GetString(row, columns, "EventType") ?? throw new InvalidOperationException("EventType is required.");
                var eventType = eventTypes.FirstOrDefault(e => e.Code.Equals(eventCode, StringComparison.OrdinalIgnoreCase))
                                 ?? throw new InvalidOperationException($"Unknown event type code '{eventCode}'.");

                Guid? supplierId = null;
                var supplierName = GetString(row, columns, "Supplier");
                if (!string.IsNullOrWhiteSpace(supplierName))
                {
                    var supplier = suppliers.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase))
                                   ?? throw new InvalidOperationException($"Unknown supplier '{supplierName}'.");
                    supplierId = supplier.Id;
                }

                var attributes = new List<ProductAttributeValueInput>();
                foreach (var header in attributeColumnHeaders)
                {
                    var optionCode = GetString(row, columns, header);
                    if (string.IsNullOrWhiteSpace(optionCode)) continue;
                    var type = attributeTypeByCode[header];
                    var option = attributeOptions.FirstOrDefault(o => o.ProductAttributeTypeId == type.Id && o.Code.Equals(optionCode, StringComparison.OrdinalIgnoreCase))
                                 ?? throw new InvalidOperationException($"Unknown option '{optionCode}' for attribute '{header}'.");
                    attributes.Add(new ProductAttributeValueInput(type.Id, option.Id));
                }

                var request = new UpsertProductRequest(
                    ItemCode: GetString(row, columns, "ItemCode"),
                    Sku: sku.Trim(),
                    Name: GetString(row, columns, "Name") ?? throw new InvalidOperationException("Name is required."),
                    Description: GetString(row, columns, "Description"),
                    ImageUrl: null,
                    DepartmentId: department.Id,
                    GenderId: gender.Id,
                    EventTypeId: eventType.Id,
                    CategoryId: category.Id,
                    SubcategoryId: subcategoryId,
                    CollectionId: null,
                    Year: GetInt(row, columns, "Year"),
                    SupplierId: supplierId,
                    Cost: GetDecimal(row, columns, "Cost") ?? throw new InvalidOperationException("Cost is required."),
                    Price: GetDecimal(row, columns, "Price") ?? throw new InvalidOperationException("Price is required."),
                    WholesalePrice: GetDecimal(row, columns, "WholesalePrice"),
                    TaxRatePercent: GetDecimal(row, columns, "TaxRatePercent") ?? 0,
                    DiscountPercent: GetDecimal(row, columns, "DiscountPercent") ?? 0,
                    Unit: GetString(row, columns, "Unit") ?? "pc",
                    MinStock: GetInt(row, columns, "MinStock") ?? 0,
                    MaxStock: GetInt(row, columns, "MaxStock"),
                    ReorderLevel: GetInt(row, columns, "ReorderLevel") ?? 10,
                    Location: GetString(row, columns, "Location"),
                    Status: GetString(row, columns, "Status") ?? "Active",
                    Attributes: attributes,
                    InitialStockWarehouseId: null,
                    InitialStockQuantity: null);

                var validation = await productValidator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                }

                Guid savedProductId;
                var isUpdate = productIdBySku.TryGetValue(request.Sku, out var existingId);
                if (isUpdate)
                {
                    await productService.UpdateAsync(existingId, request, ct);
                    savedProductId = existingId;
                }
                else
                {
                    var createdProduct = await productService.CreateAsync(request, ct);
                    savedProductId = createdProduct.Id;
                    productIdBySku[request.Sku] = createdProduct.Id;
                }

                // Optional escape hatch: if the row supplies its own Barcode (e.g. migrating a
                // legacy code already printed on physical stock), use it as-is instead of the
                // auto-computed {Sku}-{Size}-{Color} value the create/update above just assigned.
                // A conflict here (thrown to the catch below) still leaves the product saved —
                // only the barcode override itself failed — so counts are only bumped once both
                // steps succeed.
                var explicitBarcode = GetString(row, columns, "Barcode");
                if (explicitBarcode is not null)
                {
                    await barcodeService.AssignExplicitBarcodeAsync(savedProductId, explicitBarcode, ct);
                }

                if (isUpdate) updated++;
                else created++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add(new ImportRowError(rowNum, sku, ex.Message));
            }
        }

        return new ProductImportResultDto(totalRows, created, updated, failed, errors);
    }

    public async Task<InventoryImportResultDto> ImportInventoryAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheets.First();
        var columns = ReadHeaderRow(ws);

        var products = await db.Products.Select(p => new { p.Id, p.Sku }).ToListAsync(ct);
        var productIdBySku = products.ToDictionary(p => p.Sku, p => p.Id, StringComparer.OrdinalIgnoreCase);
        var warehouses = await db.Warehouses.ToListAsync(ct);

        var errors = new List<ImportRowError>();
        int applied = 0, failed = 0, totalRows = 0;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            totalRows++;
            var rowNum = row.RowNumber();
            var sku = GetString(row, columns, "SKU");

            try
            {
                if (string.IsNullOrWhiteSpace(sku)) throw new InvalidOperationException("SKU is required.");
                if (!productIdBySku.TryGetValue(sku.Trim(), out var productId))
                {
                    throw new InvalidOperationException($"No product found with SKU '{sku}'.");
                }

                var warehouseCode = GetString(row, columns, "WarehouseCode") ?? throw new InvalidOperationException("WarehouseCode is required.");
                var warehouse = warehouses.FirstOrDefault(w => w.Code.Equals(warehouseCode, StringComparison.OrdinalIgnoreCase))
                                ?? throw new InvalidOperationException($"Unknown warehouse code '{warehouseCode}'.");

                var quantity = GetInt(row, columns, "Quantity") ?? throw new InvalidOperationException("Quantity is required.");
                if (quantity < 0) throw new InvalidOperationException("Quantity cannot be negative.");

                var reason = GetString(row, columns, "Reason") ?? "Bulk import";

                await inventoryService.AdjustAsync(new AdjustStockRequest(productId, warehouse.Id, quantity, reason), userId: null, ct);
                applied++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add(new ImportRowError(rowNum, sku, ex.Message));
            }
        }

        return new InventoryImportResultDto(totalRows, applied, failed, errors);
    }

    private static Dictionary<string, int> ReadHeaderRow(IXLWorksheet ws)
    {
        var headerRow = ws.Row(1);
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (header.Length > 0)
            {
                columns[header] = cell.Address.ColumnNumber;
            }
        }
        return columns;
    }

    private static string? GetString(IXLRow row, Dictionary<string, int> columns, string header)
    {
        if (!columns.TryGetValue(header, out var col)) return null;
        var value = row.Cell(col).GetString().Trim();
        return value.Length == 0 ? null : value;
    }

    private static decimal? GetDecimal(IXLRow row, Dictionary<string, int> columns, string header)
    {
        var raw = GetString(row, columns, header);
        if (raw is null) return null;
        if (!decimal.TryParse(raw, out var value)) throw new InvalidOperationException($"'{header}' value '{raw}' is not a valid number.");
        return value;
    }

    private static int? GetInt(IXLRow row, Dictionary<string, int> columns, string header)
    {
        var raw = GetString(row, columns, header);
        if (raw is null) return null;
        if (!int.TryParse(raw, out var value)) throw new InvalidOperationException($"'{header}' value '{raw}' is not a valid whole number.");
        return value;
    }
}
