using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Sales;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Parties;
using RetailCommerce.Domain.Sales;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Sales;

/// <summary>Reimplements, as a C# Application-layer transaction, the logic the reference
/// prototype had correctly designed in a PL/pgSQL function (create_sales_order) but which its
/// POS screen never actually called. Every rule from that function is preserved: validate stock
/// across warehouses before committing, deduct preferring the selling warehouse then falling
/// back to others, record an immutable stock movement per deduction, and award loyalty points.
/// Fixed vs. the prototype: pricing/tax are always read server-side from the product (the
/// prototype's POS hardcoded a flat 5% tax regardless of each product's own tax rate), and the
/// order number comes from a real Postgres sequence instead of client-side Math.random().</summary>
public class SalesService(AppDbContext db, IDocumentNumberService documentNumbers, ICurrentUserService currentUser) : ISalesService
{
    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, Guid? cashierUserId, CancellationToken ct = default)
    {
        // A terminal-scoped cashier can only ever sell against their own terminal's warehouse —
        // this overrides/validates request.WarehouseId server-side rather than trusting it, per
        // the multi-store isolation requirement. Back-office callers (no terminal claim) are
        // unaffected, but must supply one explicitly since there's no claim to default from.
        var warehouseId = currentUser.ResolveWarehouseScope(request.WarehouseId)
            ?? throw new ConflictException("A warehouse must be specified.");
        if (!await db.Warehouses.AnyAsync(w => w.Id == warehouseId, ct))
        {
            throw new NotFoundException("Warehouse", warehouseId);
        }

        Customer? customer = null;
        if (request.CustomerId is { } customerId)
        {
            customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct)
                       ?? throw new NotFoundException("Customer", customerId);
        }

        if (request.SalesPersonId is { } salesPersonId && !await db.Employees.AnyAsync(e => e.Id == salesPersonId, ct))
        {
            throw new NotFoundException("Employee", salesPersonId);
        }

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod))
        {
            throw new ConflictException($"Unknown payment method '{request.PaymentMethod}'.");
        }

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        foreach (var line in request.Lines)
        {
            if (!products.ContainsKey(line.ProductId))
            {
                throw new NotFoundException("Product", line.ProductId);
            }
        }

        var departmentIds = products.Values.Select(p => p.DepartmentId).Distinct().ToList();
        var activeScopedDiscounts = await db.Discounts
            .Where(d => d.IsActive &&
                ((d.ProductId != null && productIds.Contains(d.ProductId.Value)) ||
                 (d.DepartmentId != null && departmentIds.Contains(d.DepartmentId.Value))))
            .ToListAsync(ct);
        // A discount can target a Department and a Product at once (e.g. "all of Footwear plus
        // this one Bag"), so the same row may appear in both lookups.
        var productDiscounts = activeScopedDiscounts.Where(d => d.ProductId != null).ToDictionary(d => d.ProductId!.Value);
        var departmentDiscounts = activeScopedDiscounts.Where(d => d.DepartmentId != null).ToDictionary(d => d.DepartmentId!.Value);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var orderNumber = await documentNumbers.NextAsync(DocumentType.SalesInvoice, ct);

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customer?.Id,
            WarehouseId = warehouseId,
            Channel = OrderChannel.Pos,
            Status = OrderStatus.Completed,
            PaymentMethod = paymentMethod,
            SalesPersonId = request.SalesPersonId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedByUserId = cashierUserId,
            TerminalId = currentUser.TerminalId,
        };

        decimal subtotal = 0, discountTotal = 0;
        var appliedDiscountLabels = new List<string>();

        foreach (var lineInput in request.Lines)
        {
            var product = products[lineInput.ProductId];

            var totalAvailable = await db.InventoryBalances
                .Where(i => i.ProductId == product.Id)
                .SumAsync(i => (int?)i.Quantity, ct) ?? 0;
            if (totalAvailable < lineInput.Quantity)
            {
                throw new ConflictException($"Insufficient stock for {product.Name}. Available: {totalAvailable}, requested: {lineInput.Quantity}.");
            }

            var lineSubtotal = product.Price * lineInput.Quantity;
            var (linePercent, lineDiscountLabel) = ResolveLineDiscount(
                product, productDiscounts, departmentDiscounts, lineSubtotal, request.DiscountPercent, request.DiscountLabel);
            if (lineDiscountLabel is not null) appliedDiscountLabels.Add(lineDiscountLabel);

            var lineDiscount = Math.Round(lineSubtotal * linePercent / 100m, 2);
            // POS sales don't add tax on top of the price — Product.TaxRatePercent is still
            // snapshotted onto the line below for record-keeping, but no tax amount is charged
            // or persisted here.
            var lineTotal = lineSubtotal - lineDiscount;

            subtotal += lineSubtotal;
            discountTotal += lineDiscount;

            order.Lines.Add(new OrderLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = lineInput.Quantity,
                UnitPrice = product.Price,
                TaxRatePercent = product.TaxRatePercent,
                DiscountPercent = linePercent,
                LineTotal = lineTotal,
            });

            var remaining = lineInput.Quantity;
            var balances = await db.InventoryBalances
                .Where(i => i.ProductId == product.Id && i.Quantity > 0)
                .OrderByDescending(i => i.WarehouseId == warehouseId)
                .ThenByDescending(i => i.Quantity)
                .ToListAsync(ct);

            foreach (var balance in balances)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, balance.Quantity);
                balance.Quantity -= take;
                remaining -= take;

                db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    WarehouseId = balance.WarehouseId,
                    QuantityDelta = -take,
                    Kind = StockMovementKind.Sale,
                    Reference = orderNumber,
                    PerformedByUserId = cashierUserId,
                });
            }

            if (remaining > 0)
            {
                // Stock moved between the availability check above and here (concurrent sale) —
                // fail the whole transaction loudly rather than oversell.
                throw new ConflictException($"Insufficient stock for {product.Name} — please refresh and try again.");
            }
        }

        order.Subtotal = subtotal;
        order.DiscountAmount = discountTotal;
        order.TaxAmount = 0m;
        order.Total = subtotal - discountTotal;
        order.DiscountLabel = appliedDiscountLabels.Count > 0 ? string.Join(", ", appliedDiscountLabels.Distinct()) : null;
        order.Payments.Add(new Payment { Method = paymentMethod, Amount = order.Total });

        db.Orders.Add(order);

        if (customer is not null)
        {
            customer.OrdersCount += 1;
            customer.LoyaltyPoints += (int)Math.Floor(order.Total);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await GetAsync(order.Id, ct);
    }

    public async Task<SaleDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Query().FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundException("Order", id);
        var cashierNames = await GetCashierNamesAsync([order], ct);
        return ToDto(order, cashierNames);
    }

    public async Task<PagedResult<SaleDto>> ListAsync(SaleListQuery query, CancellationToken ct = default)
    {
        var orders = Query();

        if (currentUser.ResolveWarehouseScope(query.WarehouseId) is { } wh) orders = orders.Where(o => o.WarehouseId == wh);
        if (query.CustomerId is { } cust) orders = orders.Where(o => o.CustomerId == cust);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, true, out var status))
        {
            orders = orders.Where(o => o.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            orders = orders.Where(o =>
                EF.Functions.ILike(o.OrderNumber, $"%{term}%") ||
                (o.Customer != null && EF.Functions.ILike(o.Customer.FirstName, $"%{term}%")));
        }

        var totalCount = await orders.CountAsync(ct);
        var page = await orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var cashierNames = await GetCashierNamesAsync(page, ct);

        return new PagedResult<SaleDto>
        {
            Items = page.Select(o => ToDto(o, cashierNames)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    /// <summary>Product-targeted discounts win over Department-targeted, which win over the
    /// manual/cart-wide discount the cashier picked at POS (if any) — see Discount.DepartmentId/
    /// ProductId. The label shows the discount's percentage when it's a Percentage-type rule
    /// (e.g. "Footwear Clearance (10%)") but not for FixedAmount (a converted-to-percent number
    /// like "29.76%" would be confusing next to a flat "Rs. 25 off" campaign).</summary>
    private static (decimal Percent, string? Label) ResolveLineDiscount(
        Domain.Catalog.Product product,
        IReadOnlyDictionary<Guid, Discount> productDiscounts,
        IReadOnlyDictionary<Guid, Discount> departmentDiscounts,
        decimal lineSubtotal,
        decimal manualPercent,
        string? manualLabel)
    {
        if (productDiscounts.TryGetValue(product.Id, out var productDiscount))
        {
            return (EffectivePercent(productDiscount, lineSubtotal, product.Price), LabelFor(productDiscount));
        }
        if (product.DepartmentId is { } deptId && departmentDiscounts.TryGetValue(deptId, out var departmentDiscount))
        {
            return (EffectivePercent(departmentDiscount, lineSubtotal, product.Price), LabelFor(departmentDiscount));
        }
        if (manualPercent > 0)
        {
            // manualLabel already carries any "(X%)" suffix the client decided on (it knows
            // whether the source was a Percentage campaign, a FixedAmount campaign, or a raw
            // manual percent entry) — used verbatim, no further suffixing here.
            return (manualPercent, string.IsNullOrWhiteSpace(manualLabel) ? "Manual discount" : manualLabel.Trim());
        }
        return (0, null);
    }

    private static string LabelFor(Discount discount) =>
        discount.Type == DiscountValueType.Percentage ? $"{discount.Name} ({discount.Value:0.##}%)" : discount.Name;

    /// <summary>Every discount type funnels into a percent-of-lineSubtotal so OrderLine can keep
    /// storing a single DiscountPercent regardless of how the discount was originally defined.
    /// FixedPrice needs the per-unit price (not lineSubtotal, which already includes quantity) to
    /// compute how big a percentage its flat final price represents.</summary>
    private static decimal EffectivePercent(Discount discount, decimal lineSubtotal, decimal unitPrice)
    {
        if (discount.Type == DiscountValueType.Percentage) return discount.Value;
        if (discount.Type == DiscountValueType.FixedPrice)
        {
            if (unitPrice <= 0) return 0;
            return Math.Min(100, Math.Max(0, (unitPrice - discount.Value) / unitPrice * 100));
        }
        if (lineSubtotal <= 0) return 0;
        return Math.Min(100, discount.Value / lineSubtotal * 100);
    }

    private IQueryable<Order> Query() =>
        db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse).ThenInclude(w => w.Store)
            .Include(o => o.SalesPerson)
            .Include(o => o.Lines);

    private async Task<Dictionary<Guid, string>> GetCashierNamesAsync(IReadOnlyList<Order> orders, CancellationToken ct)
    {
        var userIds = orders.Where(o => o.CreatedByUserId.HasValue).Select(o => o.CreatedByUserId!.Value).Distinct().ToList();
        if (userIds.Count == 0) return new Dictionary<Guid, string>();

        return await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName}{(u.LastName is { Length: > 0 } ln ? " " + ln : "")}", ct);
    }

    private static SaleDto ToDto(Order o, IReadOnlyDictionary<Guid, string> cashierNames) => new(
        o.Id, o.OrderNumber, o.CustomerId, o.Customer is null ? null : $"{o.Customer.FirstName}{(o.Customer.LastName is { Length: > 0 } ln ? " " + ln : "")}",
        o.WarehouseId, o.Warehouse.Name, o.Channel.ToString(), o.Status.ToString(),
        o.Subtotal, o.DiscountAmount, o.TaxAmount, o.Total, o.DiscountLabel,
        o.SalesPersonId, o.SalesPerson?.Name,
        o.PaymentMethod.ToString(), o.Notes,
        o.CreatedByUserId.HasValue && cashierNames.TryGetValue(o.CreatedByUserId.Value, out var name) ? name : null,
        o.Warehouse.Store is null ? null : new ReceiptStoreInfoDto(
            o.Warehouse.Store.Address, o.Warehouse.Store.Phone, o.Warehouse.Store.Email,
            o.Warehouse.Store.Ntn, o.Warehouse.Store.Strn, o.Warehouse.Store.ReceiptFooterText),
        o.Lines.Select(l => new SaleLineDto(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.TaxRatePercent, l.DiscountPercent, l.LineTotal)).ToList(),
        o.CreatedAtUtc);
}
