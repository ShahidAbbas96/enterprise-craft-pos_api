namespace RetailCommerce.Application.Reports;

/// <summary>Mirrors a "Stock On Hand" export from legacy retail POS software, column-for-column.
/// "Heels Barcode" (the source's secondary/internal barcode scheme) has no equivalent here and
/// is always null rather than fabricated.</summary>
public record StockOnHandLineDto(
    string ItemId,
    string ItemName,
    string SearchName,
    string LineItem,
    string? Color,
    string? Size,
    string Store,
    string? BarCode,
    string? HeelsBarcode,
    decimal RetailPrice,
    int OnHandQuantity,
    decimal RetailValue,
    decimal SalesPrice,
    decimal SalesValue);

public record StockOnHandTotalsDto(int OnHandQuantity, decimal RetailValue, decimal SalesValue);

public record StockOnHandReportDto(
    string StoreFilter,
    string DepartmentFilter,
    IReadOnlyList<StockOnHandLineDto> Lines,
    StockOnHandTotalsDto GrandTotal);

public class StockOnHandQuery
{
    public Guid? StoreId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Search { get; set; }
}
