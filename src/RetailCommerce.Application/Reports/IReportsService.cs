namespace RetailCommerce.Application.Reports;

public interface IReportsService
{
    Task<SalesReportDto> GetSalesReportAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int days, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseValuationDto>> GetInventoryValuationAsync(CancellationToken ct = default);
    Task<SalesDetailReportDto> GetSalesDetailReportAsync(SalesDetailReportQuery query, CancellationToken ct = default);
    Task<StockOnHandReportDto> GetStockOnHandReportAsync(StockOnHandQuery query, CancellationToken ct = default);
}
