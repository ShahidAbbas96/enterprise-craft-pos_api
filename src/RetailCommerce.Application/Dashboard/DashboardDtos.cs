namespace RetailCommerce.Application.Dashboard;

public record DailySalesPointDto(DateOnly Date, decimal Sales, decimal Profit, int Orders);

public record DepartmentMixDto(Guid DepartmentId, string DepartmentName, decimal Revenue, decimal SharePercent);

public record WarehousePerformanceDto(Guid WarehouseId, string WarehouseName, decimal Sales, int OrderCount);

public record ActivityItemDto(string Text, string? Who, DateTimeOffset AtUtc, string Tone);

/// <summary>Every figure here is a real query over Orders/OrderLines/InventoryBalances/
/// StockMovements — the reference prototype's dashboard was 100% hardcoded literals.
/// Gross profit is approximated as (unit price − current product cost) × quantity, ignoring
/// tax/discount; it will drift slightly if a product's cost has changed since the sale, which
/// is an acceptable approximation for a KPI tile, not a financial statement.</summary>
public record DashboardSummaryDto(
    decimal TodaySales,
    int TodayOrders,
    decimal TodayGrossProfit,
    decimal MtdRevenue,
    int MtdOrders,
    decimal InventoryValue,
    int TotalActiveSkus,
    int LowStockCount,
    int OutOfStockCount,
    int PendingPurchaseOrders,
    int OpenTransfers,
    int TotalCustomers,
    IReadOnlyList<DailySalesPointDto> SalesTrend,
    IReadOnlyList<DepartmentMixDto> DepartmentMix,
    IReadOnlyList<WarehousePerformanceDto> WarehousePerformance,
    IReadOnlyList<ActivityItemDto> RecentActivity);
