using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Dashboard;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Sales;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Dashboard;

/// <summary>Every figure is a real aggregation query — the reference prototype's dashboard was
/// entirely hardcoded literals (`currency(26310)` etc. baked into the component).</summary>
public class DashboardService(AppDbContext db, ICurrentUserService currentUser) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        // No query param exists for this endpoint — Dashboard has always been company-wide, so
        // the only way a terminal-scoped Cashier or a store-pinned StoreManager gets restricted
        // is via the claim itself (there is nothing for them to "request" here, so this always
        // resolves to their own store rather than ever throwing on mismatch). SuperAdmin/
        // HeadOffice (no pinned StoreId) get null back and keep seeing every store's numbers.
        var storeId = currentUser.ResolveStoreScope(null);

        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(new DateTime(now.Year, now.Month, 1), TimeSpan.Zero);
        var trendStart = todayStart.AddDays(-6);
        var windowStart = monthStart < trendStart ? monthStart : trendStart;

        var ordersQuery = db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAtUtc >= windowStart)
            .Include(o => o.Lines)
            .Include(o => o.Warehouse)
            .AsQueryable();
        if (storeId is { } sid) ordersQuery = ordersQuery.Where(o => o.Warehouse.StoreId == sid);
        var orders = await ordersQuery.ToListAsync(ct);

        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var productInfo = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductCostInfo(p.Id, p.Cost, p.DepartmentId))
            .ToDictionaryAsync(p => p.Id, ct);
        var departmentNames = await db.Departments.ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        decimal ProfitFor(IEnumerable<OrderLine> lines) =>
            lines.Sum(l => (l.UnitPrice - (productInfo.TryGetValue(l.ProductId, out var p) ? p.Cost : 0)) * l.Quantity);

        var todayOrders = orders.Where(o => o.CreatedAtUtc >= todayStart).ToList();
        var mtdOrders = orders.Where(o => o.CreatedAtUtc >= monthStart).ToList();
        var trendOrders = orders.Where(o => o.CreatedAtUtc >= trendStart).ToList();

        var salesTrend = new List<DailySalesPointDto>();
        for (var i = 6; i >= 0; i--)
        {
            var day = todayStart.AddDays(-i).Date;
            var dayOrders = trendOrders.Where(o => o.CreatedAtUtc.Date == day).ToList();
            salesTrend.Add(new DailySalesPointDto(
                DateOnly.FromDateTime(day),
                dayOrders.Sum(o => o.Total),
                ProfitFor(dayOrders.SelectMany(o => o.Lines)),
                dayOrders.Count));
        }

        var departmentMix = BuildDepartmentMix(mtdOrders, productInfo, departmentNames);
        var warehousePerformance = mtdOrders
            .GroupBy(o => o.WarehouseId)
            .Select(g => new WarehousePerformanceDto(g.Key, g.First().Warehouse.Name, g.Sum(o => o.Total), g.Count()))
            .OrderByDescending(w => w.Sales)
            .ToList();

        var (inventoryValue, totalActiveSkus, lowStockCount, outOfStockCount) = await ComputeStockPositionAsync(storeId, ct);

        // Purchasing/Transfers are back-office tools already role-gated away from Cashier/
        // StoreManager (CatalogManagers policy) and Customers are an explicitly shared,
        // company-wide list (see plan) — none of these three are store-partitioned by design.
        var pendingPurchaseOrders = await db.PurchaseOrders
            .CountAsync(po => po.Status == PurchaseOrderStatus.Submitted || po.Status == PurchaseOrderStatus.Approved, ct);
        var openTransfers = await db.Transfers
            .CountAsync(t => t.Status == TransferStatus.Draft || t.Status == TransferStatus.InTransit, ct);
        var totalCustomers = await db.Customers.CountAsync(ct);

        var recentActivity = await BuildRecentActivityAsync(storeId, ct);

        return new DashboardSummaryDto(
            TodaySales: todayOrders.Sum(o => o.Total),
            TodayOrders: todayOrders.Count,
            TodayGrossProfit: ProfitFor(todayOrders.SelectMany(o => o.Lines)),
            MtdRevenue: mtdOrders.Sum(o => o.Total),
            MtdOrders: mtdOrders.Count,
            InventoryValue: inventoryValue,
            TotalActiveSkus: totalActiveSkus,
            LowStockCount: lowStockCount,
            OutOfStockCount: outOfStockCount,
            PendingPurchaseOrders: pendingPurchaseOrders,
            OpenTransfers: openTransfers,
            TotalCustomers: totalCustomers,
            SalesTrend: salesTrend,
            DepartmentMix: departmentMix,
            WarehousePerformance: warehousePerformance,
            RecentActivity: recentActivity);
    }

    private static List<DepartmentMixDto> BuildDepartmentMix(
        List<Order> mtdOrders,
        Dictionary<Guid, ProductCostInfo> productInfo,
        Dictionary<Guid, string> departmentNames)
    {
        var groups = mtdOrders
            .SelectMany(o => o.Lines)
            .Where(l => productInfo.ContainsKey(l.ProductId))
            .GroupBy(l => productInfo[l.ProductId].DepartmentId)
            .Select(g => new { DepartmentId = g.Key, Revenue = g.Sum(l => l.LineTotal) })
            .ToList();

        var total = groups.Sum(g => g.Revenue);
        return groups
            .Select(g => new DepartmentMixDto(
                g.DepartmentId ?? Guid.Empty,
                g.DepartmentId is { } id ? departmentNames.GetValueOrDefault(id, "Unknown") : "Unassigned",
                g.Revenue,
                total > 0 ? Math.Round(g.Revenue / total * 100, 1) : 0))
            .OrderByDescending(d => d.Revenue)
            .ToList();
    }

    /// <summary>When <paramref name="storeId"/> is set (terminal-scoped or StoreManager caller),
    /// stock figures — and which SKUs even count as "active" for this KPI — are scoped to that
    /// store's own warehouses only, never leaking another store's inventory value/counts.</summary>
    private async Task<(decimal InventoryValue, int TotalActiveSkus, int LowStockCount, int OutOfStockCount)> ComputeStockPositionAsync(Guid? storeId, CancellationToken ct)
    {
        var products = await db.Products
            .Select(p => new { p.Id, p.Cost, p.ReorderLevel, p.Status })
            .ToListAsync(ct);

        var balances = db.InventoryBalances.AsQueryable();
        if (storeId is { } sid) balances = balances.Where(i => i.Warehouse.StoreId == sid);
        var stockByProduct = await balances
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        decimal inventoryValue = 0;
        var totalActiveSkus = 0;
        var lowStockCount = 0;
        var outOfStockCount = 0;

        foreach (var p in products)
        {
            var qty = stockByProduct.GetValueOrDefault(p.Id, 0);
            if (storeId is not null && qty == 0 && !stockByProduct.ContainsKey(p.Id)) continue; // not carried by this store at all
            if (p.Status == ProductStatus.Active) totalActiveSkus++;
            inventoryValue += qty * p.Cost;
            if (qty == 0) outOfStockCount++;
            else if (qty <= p.ReorderLevel) lowStockCount++;
        }

        return (inventoryValue, totalActiveSkus, lowStockCount, outOfStockCount);
    }

    private async Task<List<ActivityItemDto>> BuildRecentActivityAsync(Guid? storeId, CancellationToken ct)
    {
        var movementsQuery = db.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .AsQueryable();
        if (storeId is { } sid) movementsQuery = movementsQuery.Where(m => m.Warehouse.StoreId == sid);
        var recentMovements = await movementsQuery
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(8)
            .ToListAsync(ct);

        return recentMovements
            .Select(m => new ActivityItemDto(BuildActivityText(m), m.Warehouse.Name, m.CreatedAtUtc, ToneFor(m.Kind)))
            .ToList();
    }

    private static string BuildActivityText(StockMovement m) => m.Kind switch
    {
        StockMovementKind.Sale => $"Sale: {Math.Abs(m.QuantityDelta)}× {m.Product.Name}{(m.Reference is { Length: > 0 } r ? $" ({r})" : "")}",
        StockMovementKind.PurchaseReceipt => $"Purchase received: +{m.QuantityDelta} {m.Product.Name}{(m.Reference is { Length: > 0 } r2 ? $" ({r2})" : "")}",
        StockMovementKind.TransferIn => $"Transfer in: +{m.QuantityDelta} {m.Product.Name}{(m.Reference is { Length: > 0 } r3 ? $" ({r3})" : "")}",
        StockMovementKind.TransferOut => $"Transfer out: {m.QuantityDelta} {m.Product.Name}{(m.Reference is { Length: > 0 } r4 ? $" ({r4})" : "")}",
        StockMovementKind.Adjustment => $"Stock adjusted: {(m.QuantityDelta > 0 ? "+" : "")}{m.QuantityDelta} {m.Product.Name}{(m.Reference is { Length: > 0 } r5 ? $" ({r5})" : "")}",
        StockMovementKind.OpeningStock => $"Opening stock: +{m.QuantityDelta} {m.Product.Name}",
        _ => $"{m.Kind}: {m.QuantityDelta} {m.Product.Name}",
    };

    private static string ToneFor(StockMovementKind kind) => kind switch
    {
        StockMovementKind.Sale => "success",
        StockMovementKind.PurchaseReceipt => "info",
        StockMovementKind.TransferIn => "info",
        StockMovementKind.TransferOut => "warning",
        StockMovementKind.Adjustment => "warning",
        _ => "info",
    };

    private record ProductCostInfo(Guid Id, decimal Cost, Guid? DepartmentId);
}
