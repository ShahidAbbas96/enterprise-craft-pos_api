namespace RetailCommerce.Application.Reports;

public record SalesReportPointDto(DateOnly Date, decimal Sales, decimal Profit, int Orders);

public record SalesReportDto(
    IReadOnlyList<SalesReportPointDto> Points,
    decimal TotalSales,
    decimal TotalProfit,
    int TotalOrders,
    decimal AverageBasket,
    decimal TotalTax);

public record TopProductDto(Guid ProductId, string Sku, string? Barcode, string ProductName, int QuantitySold, decimal Revenue);

public record WarehouseValuationDto(Guid WarehouseId, string WarehouseName, int Skus, int UnitsOnHand, decimal StockValue);
