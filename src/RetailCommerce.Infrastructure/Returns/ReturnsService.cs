using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Returns;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Sales;
using RetailCommerce.Domain.Sync;
using RetailCommerce.Infrastructure.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Returns;

/// <summary>Refunds each line using the ORIGINAL sale's stored DiscountPercent/TaxRatePercent
/// (same formula SalesService used to compute LineTotal), so a full-quantity return refunds
/// exactly what was charged and a partial return is proportional — no separate refund math to
/// keep in sync.</summary>
public class ReturnsService(AppDbContext db, IDocumentNumberService documentNumbers, ICurrentUserService currentUser) : IReturnsService
{
    public async Task<ReturnableSaleDto> LookupAsync(string orderNumber, CancellationToken ct = default)
    {
        var trimmed = orderNumber.Trim();
        var order = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.OrderNumber == trimmed, ct)
            ?? throw new NotFoundException("Order", trimmed);
        // A terminal-scoped cashier can't return/exchange an invoice rung at another store, even
        // by guessing/typing its number — this throws ForbiddenException on mismatch.
        currentUser.ResolveWarehouseScope(order.WarehouseId);

        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var returnedByLine = await db.ReturnLines
            .Where(rl => lineIds.Contains(rl.OrderLineId))
            .GroupBy(rl => rl.OrderLineId)
            .Select(g => new { OrderLineId = g.Key, Quantity = g.Sum(rl => rl.Quantity) })
            .ToDictionaryAsync(x => x.OrderLineId, x => x.Quantity, ct);

        var lines = order.Lines.Select(l =>
        {
            var alreadyReturned = returnedByLine.GetValueOrDefault(l.Id);
            return new ReturnableLineDto(l.Id, l.ProductId, l.ProductName, l.Quantity, alreadyReturned, l.Quantity - alreadyReturned, l.UnitPrice, l.DiscountPercent);
        }).ToList();

        return new ReturnableSaleDto(
            order.Id, order.OrderNumber, order.WarehouseId, order.Warehouse.Name,
            order.Customer is null ? null : $"{order.Customer.FirstName}{(order.Customer.LastName is { Length: > 0 } ln ? " " + ln : "")}",
            order.CreatedAtUtc, lines);
    }

    public async Task<ReturnDto> CreateReturnAsync(CreateReturnRequest request, Guid? userId, CancellationToken ct = default)
    {
        // Fast-path idempotency pre-check — mirrors SalesService.CreateSaleAsync exactly: a
        // retried submission of an already-processed offline return short-circuits here, before
        // redoing stock/quantity validation. The filtered unique index on Return.ClientTransactionId
        // (caught below) is the real concurrency-safe guarantee against a duplicate under a race.
        if (request.ClientTransactionId is { } precheckKey)
        {
            var existingReturnId = await db.Returns
                .Where(r => r.ClientTransactionId == precheckKey)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync(ct);
            if (existingReturnId is { } duplicateReturnId)
            {
                await LogSyncAsync(duplicateReturnId, precheckKey, SyncLogStatus.Duplicate, null, ct);
                return await GetReturnDtoAsync(duplicateReturnId, ct);
            }
        }

        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
                    ?? throw new NotFoundException("Order", request.OrderId);
        currentUser.ResolveWarehouseScope(order.WarehouseId);

        var orderLines = order.Lines.ToDictionary(l => l.Id);
        foreach (var lineInput in request.Lines)
        {
            if (!orderLines.ContainsKey(lineInput.OrderLineId))
            {
                throw new ConflictException("One of the selected lines does not belong to this order.");
            }
        }

        var lineIds = request.Lines.Select(l => l.OrderLineId).ToList();
        var alreadyReturned = await db.ReturnLines
            .Where(rl => lineIds.Contains(rl.OrderLineId))
            .GroupBy(rl => rl.OrderLineId)
            .Select(g => new { OrderLineId = g.Key, Quantity = g.Sum(rl => rl.Quantity) })
            .ToDictionaryAsync(x => x.OrderLineId, x => x.Quantity, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var returnNumber = await documentNumbers.NextAsync(DocumentType.Return, ct: ct);
        var returnOrder = new Return
        {
            ReturnNumber = returnNumber,
            OrderId = order.Id,
            WarehouseId = order.WarehouseId,
            CustomerId = order.CustomerId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            CreatedByUserId = userId,
            ClientTransactionId = request.ClientTransactionId,
        };

        decimal total = 0;
        foreach (var lineInput in request.Lines)
        {
            var orderLine = orderLines[lineInput.OrderLineId];
            var returnable = orderLine.Quantity - alreadyReturned.GetValueOrDefault(orderLine.Id);
            if (lineInput.Quantity > returnable)
            {
                throw new ConflictException($"Cannot return {lineInput.Quantity} of {orderLine.ProductName} — only {returnable} returnable.");
            }

            // Mirrors SalesService.CreateSaleAsync's pricing exactly: POS sales never add tax on
            // top of the price (order.TaxAmount is always 0), so the refund must stop at the
            // discounted subtotal too — adding TaxRatePercent here would refund tax that was
            // never actually collected from the customer.
            var lineSubtotal = orderLine.UnitPrice * lineInput.Quantity;
            var lineDiscount = Math.Round(lineSubtotal * orderLine.DiscountPercent / 100m, 2);
            var lineRefund = lineSubtotal - lineDiscount;
            total += lineRefund;

            returnOrder.Lines.Add(new ReturnLine
            {
                OrderLineId = orderLine.Id,
                ProductId = orderLine.ProductId,
                ProductName = orderLine.ProductName,
                Quantity = lineInput.Quantity,
                UnitPrice = orderLine.UnitPrice,
                LineTotal = lineRefund,
            });

            var balance = await db.InventoryBalances
                .FirstOrDefaultAsync(i => i.ProductId == orderLine.ProductId && i.WarehouseId == order.WarehouseId, ct);
            if (balance is null)
            {
                balance = new InventoryBalance { ProductId = orderLine.ProductId, WarehouseId = order.WarehouseId, Quantity = 0 };
                db.InventoryBalances.Add(balance);
            }
            balance.Quantity += lineInput.Quantity;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = orderLine.ProductId,
                WarehouseId = order.WarehouseId,
                QuantityDelta = lineInput.Quantity,
                Kind = StockMovementKind.SaleReturn,
                Reference = returnNumber,
                PerformedByUserId = userId,
            });
        }

        returnOrder.Total = total;
        db.Returns.Add(returnOrder);

        var fullyReturned = order.Lines.All(l =>
        {
            var returnedForLine = alreadyReturned.GetValueOrDefault(l.Id) + request.Lines.Where(x => x.OrderLineId == l.Id).Sum(x => x.Quantity);
            return returnedForLine >= l.Quantity;
        });
        if (fullyReturned)
        {
            order.Status = OrderStatus.Refunded;
        }

        if (request.ClientTransactionId is not null)
        {
            db.SyncLogs.Add(new SyncLog
            {
                TerminalId = currentUser.TerminalId,
                Direction = SyncDirection.Push,
                EntityType = "Return",
                EntityId = returnOrder.Id,
                ClientTransactionId = request.ClientTransactionId,
                Status = SyncLogStatus.Success,
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolationOn("IX_Returns_ClientTransactionId"))
        {
            // Real safety net: a concurrent retry of the same offline return raced this one past
            // the fast-path pre-check above — roll back this attempt and return whichever one the
            // database accepted, exactly like SalesService.CreateSaleAsync.
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            var winningReturnId = await db.Returns
                .Where(r => r.ClientTransactionId == request.ClientTransactionId)
                .Select(r => r.Id)
                .FirstAsync(ct);
            await LogSyncAsync(winningReturnId, request.ClientTransactionId, SyncLogStatus.Duplicate, null, ct);
            return await GetReturnDtoAsync(winningReturnId, ct);
        }

        return await GetReturnDtoAsync(returnOrder.Id, ct);
    }

    private async Task LogSyncAsync(Guid returnId, Guid? clientTransactionId, SyncLogStatus status, string? errorMessage, CancellationToken ct)
    {
        db.SyncLogs.Add(new SyncLog
        {
            TerminalId = currentUser.TerminalId,
            Direction = SyncDirection.Push,
            EntityType = "Return",
            EntityId = returnId,
            ClientTransactionId = clientTransactionId,
            Status = status,
            ErrorMessage = errorMessage,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ReturnDto>> ListAsync(ReturnListQuery query, CancellationToken ct = default)
    {
        var returns = Query();
        if (currentUser.ResolveWarehouseScope(query.WarehouseId) is { } wh) returns = returns.Where(r => r.WarehouseId == wh);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            returns = returns.Where(r => EF.Functions.ILike(r.ReturnNumber, $"%{term}%") || EF.Functions.ILike(r.Order.OrderNumber, $"%{term}%"));
        }

        var totalCount = await returns.CountAsync(ct);
        var page = await returns
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var names = await GetUserNamesAsync(page, ct);

        return new PagedResult<ReturnDto>
        {
            Items = page.Select(r => ToDto(r, names)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    private IQueryable<Return> Query() =>
        db.Returns.Include(r => r.Order).Include(r => r.Warehouse).Include(r => r.Customer).Include(r => r.Lines);

    private async Task<ReturnDto> GetReturnDtoAsync(Guid id, CancellationToken ct)
    {
        var entity = await Query().FirstAsync(r => r.Id == id, ct);
        var names = await GetUserNamesAsync([entity], ct);
        return ToDto(entity, names);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyList<Return> returns, CancellationToken ct)
    {
        var userIds = returns.Where(r => r.CreatedByUserId.HasValue).Select(r => r.CreatedByUserId!.Value).Distinct().ToList();
        if (userIds.Count == 0) return new Dictionary<Guid, string>();

        return await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName}{(u.LastName is { Length: > 0 } ln ? " " + ln : "")}", ct);
    }

    private static ReturnDto ToDto(Return r, IReadOnlyDictionary<Guid, string> names) => new(
        r.Id, r.ReturnNumber, r.OrderId, r.Order.OrderNumber, r.WarehouseId, r.Warehouse.Name,
        r.Customer is null ? null : $"{r.Customer.FirstName}{(r.Customer.LastName is { Length: > 0 } ln ? " " + ln : "")}",
        r.Reason, r.Total,
        r.CreatedByUserId.HasValue && names.TryGetValue(r.CreatedByUserId.Value, out var name) ? name : null,
        r.Lines.Select(l => new ReturnLineDto(l.OrderLineId, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
        r.CreatedAtUtc,
        r.ClientTransactionId);
}
