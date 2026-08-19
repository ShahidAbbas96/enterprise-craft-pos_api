using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Inventory;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Inventory;

/// <summary>Cycle-count / manual adjustment workflow — entirely absent from the reference
/// prototype (its Inventory screen was 100% hardcoded mock data with a "Cycle Count" button
/// that had no click handler). Every adjustment here reconciles a physically counted quantity
/// against the system quantity and writes the delta as an immutable StockMovement.</summary>
public class InventoryService(AppDbContext db, ICurrentUserService currentUser) : IInventoryService
{
    public async Task<PagedResult<InventoryBalanceDto>> ListAsync(InventoryListQuery query, CancellationToken ct = default)
    {
        var balances = db.InventoryBalances
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .AsQueryable();

        if (currentUser.ResolveWarehouseScope(query.WarehouseId) is { } wh) balances = balances.Where(i => i.WarehouseId == wh);
        if (query.ProductId is { } pid) balances = balances.Where(i => i.ProductId == pid);
        if (query.LowStockOnly) balances = balances.Where(i => i.Quantity <= i.Product.ReorderLevel);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            balances = balances.Where(i =>
                EF.Functions.ILike(i.Product.Name, $"%{term}%") ||
                EF.Functions.ILike(i.Product.Sku, $"%{term}%"));
        }

        var totalCount = await balances.CountAsync(ct);
        var page = await balances
            .OrderBy(i => i.Product.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<InventoryBalanceDto>
        {
            Items = page.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<InventoryBalanceDto> AdjustAsync(AdjustStockRequest request, Guid? userId, CancellationToken ct = default)
    {
        var warehouseId = currentUser.ResolveWarehouseScope(request.WarehouseId);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
                      ?? throw new NotFoundException("Product", request.ProductId);
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId, ct)
                        ?? throw new NotFoundException("Warehouse", warehouseId);

        var balance = await db.InventoryBalances
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == warehouseId, ct);

        var currentQuantity = balance?.Quantity ?? 0;
        var delta = request.CountedQuantity - currentQuantity;

        if (balance is null)
        {
            balance = new InventoryBalance { ProductId = request.ProductId, WarehouseId = warehouseId, Quantity = request.CountedQuantity };
            db.InventoryBalances.Add(balance);
        }
        else
        {
            balance.Quantity = request.CountedQuantity;
        }

        if (delta != 0)
        {
            db.StockMovements.Add(new StockMovement
            {
                ProductId = request.ProductId,
                WarehouseId = warehouseId,
                QuantityDelta = delta,
                Kind = StockMovementKind.Adjustment,
                Reference = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                PerformedByUserId = userId,
            });
        }

        await db.SaveChangesAsync(ct);

        return new InventoryBalanceDto(
            balance.Id, product.Id, product.Sku, product.Name, product.ItemCode, product.Barcode,
            warehouse.Id, warehouse.Name, balance.Quantity, product.MinStock, product.ReorderLevel,
            balance.Quantity <= product.ReorderLevel);
    }

    public async Task<PagedResult<StockMovementDto>> ListMovementsAsync(StockMovementListQuery query, CancellationToken ct = default)
    {
        var movements = db.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .AsQueryable();

        if (currentUser.ResolveWarehouseScope(query.WarehouseId) is { } wh) movements = movements.Where(m => m.WarehouseId == wh);
        if (query.ProductId is { } pid) movements = movements.Where(m => m.ProductId == pid);
        if (!string.IsNullOrWhiteSpace(query.Kind) && Enum.TryParse<StockMovementKind>(query.Kind, true, out var kind))
        {
            movements = movements.Where(m => m.Kind == kind);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            movements = movements.Where(m => EF.Functions.ILike(m.Product.Name, $"%{term}%") || EF.Functions.ILike(m.Product.Sku, $"%{term}%"));
        }

        var totalCount = await movements.CountAsync(ct);
        var page = await movements
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<StockMovementDto>
        {
            Items = page.Select(m => new StockMovementDto(
                m.Id, m.ProductId, m.Product.Sku, m.Product.Name, m.WarehouseId, m.Warehouse.Name,
                m.QuantityDelta, m.Kind.ToString(), m.Reference, m.CreatedAtUtc)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    private static InventoryBalanceDto ToDto(InventoryBalance b) => new(
        b.Id, b.ProductId, b.Product.Sku, b.Product.Name, b.Product.ItemCode, b.Product.Barcode,
        b.WarehouseId, b.Warehouse.Name, b.Quantity, b.Product.MinStock, b.Product.ReorderLevel,
        b.Quantity <= b.Product.ReorderLevel);
}
