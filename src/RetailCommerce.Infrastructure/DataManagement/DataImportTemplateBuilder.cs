using ClosedXML.Excel;

namespace RetailCommerce.Infrastructure.DataManagement;

/// <summary>Blank starter workbooks with just the header row, so an import file's column names
/// are never guessed. Attribute-type columns (e.g. COLOR, SIZE) aren't baked in here since they
/// vary per deployment — the product import accepts any extra column whose header matches an
/// existing ProductAttributeType.Code, with the cell value being that attribute's option Code.</summary>
public static class DataImportTemplateBuilder
{
    private static readonly string[] ProductColumns =
    [
        "SKU", "ItemCode", "Barcode", "Name", "Description", "Department", "Category", "Subcategory", "Gender", "EventType",
        "Year", "Supplier", "Cost", "Price", "WholesalePrice", "TaxRatePercent", "DiscountPercent", "Unit",
        "MinStock", "MaxStock", "ReorderLevel", "Location", "Status", "COLOR", "SIZE",
    ];

    private static readonly string[] InventoryColumns = ["SKU", "WarehouseCode", "Quantity", "Reason"];

    public static XLWorkbook BuildProductTemplate() => BuildTemplate("Products", ProductColumns);

    public static XLWorkbook BuildInventoryTemplate() => BuildTemplate("Inventory", InventoryColumns);

    private static XLWorkbook BuildTemplate(string sheetName, string[] columns)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        for (var c = 0; c < columns.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
        }
        ws.Columns().AdjustToContents(1, 60);
        ws.SheetView.FreezeRows(1);
        return wb;
    }
}
