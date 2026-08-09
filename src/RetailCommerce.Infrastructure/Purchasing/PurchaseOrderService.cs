using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Purchasing;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Purchasing;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Purchasing;

/// <summary>Reimplements, as C# Application-layer transactions, the two-phase procurement
/// workflow the reference prototype only half-built: its PurchaseOrderDialog correctly created
/// draft/submitted orders without touching stock, and its SQL had a working
/// receive_purchase_order function — but the dialog was never wired to any button and nothing
/// ever called Receive. Here both halves exist and are reachable.</summary>
public class PurchaseOrderService(AppDbContext db, IDocumentNumberService documentNumbers) : IPurchaseOrderService
{
    private static readonly HashSet<PurchaseOrderStatus> Terminal =
        [PurchaseOrderStatus.Received, PurchaseOrderStatus.Completed, PurchaseOrderStatus.Cancelled];

    public async Task<PagedResult<PurchaseOrderDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken ct = default)
    {
        var orders = Query();

        if (query.SupplierId is { } sup) orders = orders.Where(o => o.SupplierId == sup);
        if (query.WarehouseId is { } wh) orders = orders.Where(o => o.WarehouseId == wh);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PurchaseOrderStatus>(query.Status, true, out var status))
        {
            orders = orders.Where(o => o.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            orders = orders.Where(o => EF.Functions.ILike(o.PoNumber, $"%{term}%"));
        }

        var totalCount = await orders.CountAsync(ct);
        var page = await orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<PurchaseOrderDto>
        {
            Items = page.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Query().FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundException("PurchaseOrder", id);
        return ToDto(order);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, Guid? userId, CancellationToken ct = default)
    {
        if (!await db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, ct)) throw new NotFoundException("Supplier", request.SupplierId);
        var poWarehouse = await db.Warehouses.Include(w => w.Store).FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct)
                           ?? throw new NotFoundException("Warehouse", request.WarehouseId);

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        foreach (var line in request.Lines)
        {
            if (!products.ContainsKey(line.ProductId)) throw new NotFoundException("Product", line.ProductId);
        }

        var storeSegment = poWarehouse.Store?.Code ?? poWarehouse.Code;
        var poNumber = await documentNumbers.NextAsync(DocumentType.PurchaseOrder, storeSegment, ct);

        var order = new PurchaseOrder
        {
            PoNumber = poNumber,
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            OrderDate = request.OrderDate,
            ExpectedDate = request.ExpectedDate,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = request.Submit ? PurchaseOrderStatus.Submitted : PurchaseOrderStatus.Draft,
            CreatedByUserId = userId,
        };

        decimal subtotal = 0, discountTotal = 0, taxTotal = 0;
        foreach (var lineInput in request.Lines)
        {
            var product = products[lineInput.ProductId];
            var gross = lineInput.UnitCost * lineInput.Quantity;
            var discount = Math.Round(gross * lineInput.DiscountPercent / 100m, 2);
            var taxable = gross - discount;
            var tax = Math.Round(taxable * lineInput.TaxPercent / 100m, 2);
            var lineTotal = taxable + tax;

            subtotal += gross;
            discountTotal += discount;
            taxTotal += tax;

            order.Lines.Add(new PurchaseOrderLine
            {
                ProductId = product.Id,
                Sku = product.Sku,
                Quantity = lineInput.Quantity,
                UnitCost = lineInput.UnitCost,
                DiscountPercent = lineInput.DiscountPercent,
                TaxPercent = lineInput.TaxPercent,
                LineTotal = lineTotal,
            });
        }

        order.Subtotal = subtotal;
        order.DiscountAmount = discountTotal;
        order.TaxAmount = taxTotal;
        order.Total = subtotal - discountTotal + taxTotal;

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);

        return await GetAsync(order.Id, ct);
    }

    public async Task<PurchaseOrderDto> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundException("PurchaseOrder", id);
        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var next))
        {
            throw new ConflictException($"Unknown status '{status}'.");
        }
        if (next == PurchaseOrderStatus.Received)
        {
            throw new ConflictException("Use the Receive action to mark a purchase order as received — it also updates inventory.");
        }
        if (Terminal.Contains(order.Status))
        {
            throw new ConflictException($"Purchase order {order.PoNumber} is already {order.Status} and cannot change status.");
        }

        order.Status = next;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> ReceiveAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw new NotFoundException("PurchaseOrder", id);

        if (Terminal.Contains(order.Status))
        {
            throw new ConflictException($"Purchase order {order.PoNumber} is already {order.Status}.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        foreach (var line in order.Lines)
        {
            var balance = await db.InventoryBalances
                .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.WarehouseId == order.WarehouseId, ct);
            if (balance is null)
            {
                balance = new InventoryBalance { ProductId = line.ProductId, WarehouseId = order.WarehouseId, Quantity = 0 };
                db.InventoryBalances.Add(balance);
            }
            balance.Quantity += line.Quantity;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                WarehouseId = order.WarehouseId,
                QuantityDelta = line.Quantity,
                Kind = StockMovementKind.PurchaseReceipt,
                Reference = order.PoNumber,
                PerformedByUserId = userId,
            });
        }

        order.Status = PurchaseOrderStatus.Received;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await GetAsync(id, ct);
    }

    private IQueryable<PurchaseOrder> Query() =>
        db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines).ThenInclude(l => l.Product);

    private static PurchaseOrderDto ToDto(PurchaseOrder o) => new(
        o.Id, o.PoNumber, o.SupplierId, o.Supplier.Name, o.WarehouseId, o.Warehouse.Name,
        o.OrderDate, o.ExpectedDate, o.Reference, o.Notes, o.Status.ToString(),
        o.Subtotal, o.DiscountAmount, o.TaxAmount, o.Total,
        o.Lines.Select(l => new PurchaseOrderLineDto(l.ProductId, l.Sku, l.Product.Name, l.Quantity, l.UnitCost, l.DiscountPercent, l.TaxPercent, l.LineTotal)).ToList(),
        o.CreatedAtUtc);
}
