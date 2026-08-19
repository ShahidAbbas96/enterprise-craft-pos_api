using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Reports;
using RetailCommerce.Domain.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Reports;

/// <summary>Every report here is a real query over Orders/OrderLines/InventoryBalances — the
/// reference prototype's Reports screen had zero real data behind any of its report tiles.</summary>
public class ReportsService(AppDbContext db, ICurrentUserService currentUser) : IReportsService
{
    public async Task<SalesReportDto> GetSalesReportAsync(int days, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 365);
        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Date, TimeSpan.Zero).AddDays(-(days - 1));

        var orders = await db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAtUtc >= start)
            .Include(o => o.Lines)
            .ToListAsync(ct);

        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var costs = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Cost, ct);

        var points = new List<SalesReportPointDto>();
        for (var i = days - 1; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            var dayOrders = orders.Where(o => o.CreatedAtUtc.Date == day).ToList();
            var profit = dayOrders.SelectMany(o => o.Lines).Sum(l => (l.UnitPrice - costs.GetValueOrDefault(l.ProductId)) * l.Quantity);
            points.Add(new SalesReportPointDto(DateOnly.FromDateTime(day), dayOrders.Sum(o => o.Total), profit, dayOrders.Count));
        }

        var totalSales = orders.Sum(o => o.Total);
        var totalOrders = orders.Count;

        return new SalesReportDto(
            Points: points,
            TotalSales: totalSales,
            TotalProfit: points.Sum(p => p.Profit),
            TotalOrders: totalOrders,
            AverageBasket: totalOrders > 0 ? Math.Round(totalSales / totalOrders, 2) : 0,
            TotalTax: orders.Sum(o => o.TaxAmount));
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int days, int limit, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 365);
        limit = Math.Clamp(limit, 1, 50);
        var start = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero).AddDays(-(days - 1));

        var grouped = await db.OrderLines
            .Where(l => l.Order.Status == OrderStatus.Completed && l.Order.CreatedAtUtc >= start)
            .GroupBy(l => new { l.ProductId, l.Product.Sku, l.Product.Barcode, l.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Sku, g.Key.Barcode, g.Key.Name, Quantity = g.Sum(l => l.Quantity), Revenue = g.Sum(l => l.LineTotal) })
            .OrderByDescending(p => p.Revenue)
            .Take(limit)
            .ToListAsync(ct);

        return grouped.Select(g => new TopProductDto(g.ProductId, g.Sku, g.Barcode, g.Name, g.Quantity, g.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<WarehouseValuationDto>> GetInventoryValuationAsync(CancellationToken ct = default)
    {
        var rows = await db.InventoryBalances
            .Include(i => i.Warehouse)
            .Include(i => i.Product)
            .Where(i => i.Quantity > 0)
            .ToListAsync(ct);

        return rows
            .GroupBy(i => new { i.WarehouseId, i.Warehouse.Name })
            .Select(g => new WarehouseValuationDto(
                g.Key.WarehouseId,
                g.Key.Name,
                g.Select(i => i.ProductId).Distinct().Count(),
                g.Sum(i => i.Quantity),
                g.Sum(i => i.Quantity * i.Product.Cost)))
            .OrderByDescending(w => w.StockValue)
            .ToList();
    }

    public async Task<SalesDetailReportDto> GetSalesDetailReportAsync(SalesDetailReportQuery query, CancellationToken ct = default)
    {
        var toDate = query.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = query.FromDate ?? toDate;
        var start = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var linesQuery = db.OrderLines
            .Include(l => l.Order).ThenInclude(o => o.Customer)
            .Include(l => l.Order).ThenInclude(o => o.Warehouse).ThenInclude(w => w.Store)
            .Include(l => l.Order).ThenInclude(o => o.SalesPerson)
            .Include(l => l.Product).ThenInclude(p => p.Department)
            .Include(l => l.Product).ThenInclude(p => p.Category)
            .Include(l => l.Product).ThenInclude(p => p.Subcategory)
            .Include(l => l.Product).ThenInclude(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeType)
            .Include(l => l.Product).ThenInclude(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeOption)
            .Where(l => l.Order.CreatedAtUtc >= start && l.Order.CreatedAtUtc <= end)
            .AsQueryable();

        // Resolved before filtering (not just validated) so a terminal-scoped POS caller's report
        // is silently pinned to its own store/warehouse/terminal with no filter UI needed
        // (requirement: POS reports auto-scope); a StoreManager is pinned to their store but free
        // to filter within it; SuperAdmin/HeadOffice remain fully unrestricted.
        var storeId = currentUser.ResolveStoreScope(query.StoreId);
        var warehouseId = currentUser.ResolveWarehouseScope(query.WarehouseId);
        var terminalId = currentUser.ResolveTerminalScope(query.TerminalId);

        if (storeId is { } st) linesQuery = linesQuery.Where(l => l.Order.Warehouse.StoreId == st);
        if (warehouseId is { } wh) linesQuery = linesQuery.Where(l => l.Order.WarehouseId == wh);
        if (terminalId is { } tid) linesQuery = linesQuery.Where(l => l.Order.TerminalId == tid);
        if (query.DepartmentId is { } dept) linesQuery = linesQuery.Where(l => l.Product.DepartmentId == dept);

        var lines = await linesQuery.OrderBy(l => l.Order.CreatedAtUtc).ToListAsync(ct);

        var lineIds = lines.Select(l => l.Id).ToList();
        var returnTotals = await db.ReturnLines
            .Where(rl => lineIds.Contains(rl.OrderLineId))
            .GroupBy(rl => rl.OrderLineId)
            .Select(g => new { OrderLineId = g.Key, Qty = g.Sum(rl => rl.Quantity), Sales = g.Sum(rl => rl.LineTotal) })
            .ToDictionaryAsync(x => x.OrderLineId, x => (x.Qty, x.Sales), ct);

        var dtos = lines.Select(l => BuildSalesDetailLine(l, returnTotals.GetValueOrDefault(l.Id))).ToList();

        var warehouseName = warehouseId is { } whId
            ? await db.Warehouses.Where(w => w.Id == whId).Select(w => w.Name).FirstOrDefaultAsync(ct) ?? "All"
            : "All";
        var departmentName = query.DepartmentId is { } deptId
            ? await db.Departments.Where(d => d.Id == deptId).Select(d => d.Name).FirstOrDefaultAsync(ct) ?? "All"
            : "All";

        var grandTotal = new SalesDetailTotalsDto(
            dtos.Sum(d => d.GrossQty), dtos.Sum(d => d.GrossSales), dtos.Sum(d => d.GrossSalesExcludingTax),
            dtos.Sum(d => d.ReturnQty), dtos.Sum(d => d.ReturnSales), dtos.Sum(d => d.ReturnSalesExcludingTax),
            dtos.Sum(d => d.TotalDiscountAmount), dtos.Sum(d => d.TotalDiscountExcludingTax), dtos.Sum(d => d.TaxAmount),
            dtos.Sum(d => d.NetQty), dtos.Sum(d => d.NetSale), dtos.Sum(d => d.NetSaleExcludingTax));

        return new SalesDetailReportDto(fromDate, toDate, warehouseName, departmentName, dtos, grandTotal);
    }

    private static SalesDetailLineDto BuildSalesDetailLine(Domain.Sales.OrderLine l, (int Qty, decimal Sales) ret)
    {
        var order = l.Order;
        var product = l.Product;
        var color = product.AttributeValues.FirstOrDefault(v => v.ProductAttributeType.Code == "COLOR")?.ProductAttributeOption.Name;
        var size = product.AttributeValues.FirstOrDefault(v => v.ProductAttributeType.Code == "SIZE")?.ProductAttributeOption.Name;

        var lineSubtotal = l.UnitPrice * l.Quantity;
        var lineDiscount = Math.Round(lineSubtotal * l.DiscountPercent / 100m, 2);
        var lineTaxable = lineSubtotal - lineDiscount;
        var taxAmount = l.LineTotal - lineTaxable;

        var returnQty = ret.Qty;
        var returnSales = ret.Sales;
        var qtyRatio = l.Quantity > 0 ? (decimal)returnQty / l.Quantity : 0;
        var returnTaxable = Math.Round(lineTaxable * qtyRatio, 2);
        var netTaxable = lineTaxable - returnTaxable;

        var storeId = order.Warehouse.Store?.Code ?? order.Warehouse.Code;
        var storeName = order.Warehouse.Store?.Name ?? order.Warehouse.Name;

        return new SalesDetailLineDto(
            Department: product.Department?.Name ?? "",
            Category: product.Category?.Name ?? "",
            SubCategory: product.Subcategory?.Name ?? "",
            Family: null,
            Range: null,
            ReceiptId: order.OrderNumber,
            Phone: order.Customer?.Phone,
            StoreId: storeId,
            StoreName: storeName,
            Date: DateOnly.FromDateTime(order.CreatedAtUtc.Date),
            TransactionId: null,
            SalesRepId: order.SalesPerson?.Code,
            SalesRepName: order.SalesPerson?.Name,
            TransactionComments: order.Notes,
            DiscountName: order.DiscountLabel,
            Item: product.Sku,
            ItemDescription: product.Name,
            Color: color,
            Size: size,
            Barcode: product.Barcode,
            SoldPrice: l.UnitPrice,
            GrossQty: l.Quantity,
            GrossSales: lineSubtotal,
            GrossSalesExcludingTax: lineSubtotal,
            ReturnQty: returnQty,
            ReturnSales: returnSales,
            ReturnSalesExcludingTax: returnTaxable,
            DiscountPercentage: l.DiscountPercent,
            PromotionDiscount: 0,
            ManualDiscountReason: null,
            AdjDiscPercent: 0,
            AdjDiscount: 0,
            TotalDiscountAmount: lineDiscount,
            TotalDiscountExcludingTax: lineDiscount,
            LoyaltyDiscount: 0,
            TaxAmount: taxAmount,
            TaxAdjustment: 0,
            NetQty: l.Quantity - returnQty,
            NetSale: l.LineTotal - returnSales,
            NetSaleExcludingTax: netTaxable);
    }

    public async Task<StockOnHandReportDto> GetStockOnHandReportAsync(StockOnHandQuery query, CancellationToken ct = default)
    {
        var balancesQuery = db.InventoryBalances
            .Include(i => i.Warehouse).ThenInclude(w => w.Store)
            .Include(i => i.Product).ThenInclude(p => p.Category)
            .Include(i => i.Product).ThenInclude(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeType)
            .Include(i => i.Product).ThenInclude(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeOption)
            .Where(i => i.Quantity > 0)
            .AsQueryable();

        var storeId = currentUser.ResolveStoreScope(query.StoreId);
        var warehouseId = currentUser.ResolveWarehouseScope(query.WarehouseId);

        if (storeId is { } st) balancesQuery = balancesQuery.Where(i => i.Warehouse.StoreId == st);
        if (warehouseId is { } wh) balancesQuery = balancesQuery.Where(i => i.WarehouseId == wh);
        if (query.DepartmentId is { } dept) balancesQuery = balancesQuery.Where(i => i.Product.DepartmentId == dept);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            balancesQuery = balancesQuery.Where(i =>
                EF.Functions.ILike(i.Product.Name, $"%{term}%") ||
                EF.Functions.ILike(i.Product.Sku, $"%{term}%") ||
                (i.Product.ItemCode != null && EF.Functions.ILike(i.Product.ItemCode, $"%{term}%")));
        }

        var balances = await balancesQuery.OrderBy(i => i.Product.Sku).ToListAsync(ct);

        var dtos = balances.Select(BuildStockOnHandLine).ToList();

        var warehouseName = warehouseId is { } whId
            ? await db.Warehouses.Where(w => w.Id == whId).Select(w => w.Name).FirstOrDefaultAsync(ct) ?? "All"
            : "All";
        var departmentName = query.DepartmentId is { } deptId
            ? await db.Departments.Where(d => d.Id == deptId).Select(d => d.Name).FirstOrDefaultAsync(ct) ?? "All"
            : "All";

        var grandTotal = new StockOnHandTotalsDto(
            dtos.Sum(d => d.OnHandQuantity), dtos.Sum(d => d.RetailValue), dtos.Sum(d => d.SalesValue));

        return new StockOnHandReportDto(warehouseName, departmentName, dtos, grandTotal);
    }

    private static StockOnHandLineDto BuildStockOnHandLine(Domain.Inventory.InventoryBalance i)
    {
        var product = i.Product;
        var color = product.AttributeValues.FirstOrDefault(v => v.ProductAttributeType.Code == "COLOR")?.ProductAttributeOption.Name;
        var size = product.AttributeValues.FirstOrDefault(v => v.ProductAttributeType.Code == "SIZE")?.ProductAttributeOption.Name;
        var salesPrice = product.WholesalePrice ?? product.Price;
        var storeName = i.Warehouse.Store?.Name ?? i.Warehouse.Name;

        return new StockOnHandLineDto(
            ItemId: product.ItemCode ?? product.Sku,
            ItemName: product.Name,
            SearchName: $"{product.Sku} {product.ItemCode}".Trim(),
            LineItem: product.Category?.Name ?? "",
            Color: color,
            Size: size,
            Store: storeName,
            BarCode: product.Barcode,
            HeelsBarcode: null,
            RetailPrice: product.Price,
            OnHandQuantity: i.Quantity,
            RetailValue: product.Price * i.Quantity,
            SalesPrice: salesPrice,
            SalesValue: salesPrice * i.Quantity);
    }
}
