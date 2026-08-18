using ClosedXML.Excel;

namespace RetailCommerce.Infrastructure.DataManagement;

/// <summary>Blank starter workbooks with just the header row, so an import file's column names
/// are never guessed.</summary>
public static class DataImportTemplateBuilder
{
    private static readonly string[] InventoryColumns = ["SKU", "WarehouseCode", "Quantity", "Reason"];

    /// <summary>Product columns are data-driven (Settings → Product Fields + active attribute
    /// types) rather than a fixed list here — see DataImportService.BuildProductTemplateAsync,
    /// which computes `columns` and is the only caller.</summary>
    public static XLWorkbook BuildProductTemplate(IReadOnlyList<string> columns) => BuildTemplate("Products", columns);

    public static XLWorkbook BuildInventoryTemplate() => BuildTemplate("Inventory", InventoryColumns);

    private static XLWorkbook BuildTemplate(string sheetName, IReadOnlyList<string> columns)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        for (var c = 0; c < columns.Count; c++)
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
