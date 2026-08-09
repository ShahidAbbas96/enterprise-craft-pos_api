using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Purchasing;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Purchasing;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Purchasing;

/// <summary>Reimplements complete_transfer from the reference prototype's SQL as a C#
/// Application-layer transaction: creating a transfer never moves stock, only Complete does,
/// and it fails loudly (not silently) if the source warehouse doesn't have enough on hand.</summary>
public class TransferService(AppDbContext db, IDocumentNumberService documentNumbers) : ITransferService
{
    public async Task<PagedResult<TransferDto>> ListAsync(TransferListQuery query, CancellationToken ct = default)
    {
        var transfers = Query();

        if (query.FromWarehouseId is { } from) transfers = transfers.Where(t => t.FromWarehouseId == from);
        if (query.ToWarehouseId is { } to) transfers = transfers.Where(t => t.ToWarehouseId == to);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TransferStatus>(query.Status, true, out var status))
        {
            transfers = transfers.Where(t => t.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            transfers = transfers.Where(t => EF.Functions.ILike(t.TransferNumber, $"%{term}%"));
        }

        var totalCount = await transfers.CountAsync(ct);
        var page = await transfers
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TransferDto>
        {
            Items = page.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<TransferDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var transfer = await Query().FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Transfer", id);
        return ToDto(transfer);
    }

    public async Task<TransferDto> CreateAsync(CreateTransferRequest request, Guid? userId, CancellationToken ct = default)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            throw new ConflictException("Source and destination warehouse must be different.");
        }
        if (!await db.Warehouses.AnyAsync(w => w.Id == request.FromWarehouseId, ct)) throw new NotFoundException("Warehouse", request.FromWarehouseId);
        if (!await db.Warehouses.AnyAsync(w => w.Id == request.ToWarehouseId, ct)) throw new NotFoundException("Warehouse", request.ToWarehouseId);

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingProductIds = await db.Products.Where(p => productIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct);
        foreach (var line in request.Lines)
        {
            if (!existingProductIds.Contains(line.ProductId)) throw new NotFoundException("Product", line.ProductId);
        }

        var transferNumber = await documentNumbers.NextAsync(DocumentType.Transfer, ct: ct);

        var transfer = new Transfer
        {
            TransferNumber = transferNumber,
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            TransferDate = request.TransferDate,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = TransferStatus.Draft,
            CreatedByUserId = userId,
        };

        foreach (var line in request.Lines)
        {
            transfer.Lines.Add(new TransferLine { ProductId = line.ProductId, Quantity = line.Quantity, Unit = line.Unit });
        }

        db.Transfers.Add(transfer);
        await db.SaveChangesAsync(ct);

        return await GetAsync(transfer.Id, ct);
    }

    public async Task<TransferDto> CompleteAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var transfer = await db.Transfers.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == id, ct)
                       ?? throw new NotFoundException("Transfer", id);

        if (transfer.Status is TransferStatus.Completed or TransferStatus.Cancelled)
        {
            throw new ConflictException($"Transfer {transfer.TransferNumber} is already {transfer.Status}.");
        }

        await using var dbTransaction = await db.Database.BeginTransactionAsync(ct);

        foreach (var line in transfer.Lines)
        {
            var source = await db.InventoryBalances
                .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.WarehouseId == transfer.FromWarehouseId, ct);
            if (source is null || source.Quantity < line.Quantity)
            {
                var productName = await db.Products.Where(p => p.Id == line.ProductId).Select(p => p.Name).FirstOrDefaultAsync(ct);
                throw new ConflictException($"Insufficient stock for {productName} in the source warehouse. Available: {source?.Quantity ?? 0}, requested: {line.Quantity}.");
            }
            source.Quantity -= line.Quantity;

            var destination = await db.InventoryBalances
                .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.WarehouseId == transfer.ToWarehouseId, ct);
            if (destination is null)
            {
                destination = new InventoryBalance { ProductId = line.ProductId, WarehouseId = transfer.ToWarehouseId, Quantity = 0 };
                db.InventoryBalances.Add(destination);
            }
            destination.Quantity += line.Quantity;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                WarehouseId = transfer.FromWarehouseId,
                QuantityDelta = -line.Quantity,
                Kind = StockMovementKind.TransferOut,
                Reference = transfer.TransferNumber,
                PerformedByUserId = userId,
            });
            db.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                WarehouseId = transfer.ToWarehouseId,
                QuantityDelta = line.Quantity,
                Kind = StockMovementKind.TransferIn,
                Reference = transfer.TransferNumber,
                PerformedByUserId = userId,
            });
        }

        transfer.Status = TransferStatus.Completed;
        await db.SaveChangesAsync(ct);
        await dbTransaction.CommitAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<TransferDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var transfer = await db.Transfers.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Transfer", id);
        if (transfer.Status is TransferStatus.Completed or TransferStatus.Cancelled)
        {
            throw new ConflictException($"Transfer {transfer.TransferNumber} is already {transfer.Status}.");
        }
        transfer.Status = TransferStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private IQueryable<Transfer> Query() =>
        db.Transfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Lines).ThenInclude(l => l.Product);

    private static TransferDto ToDto(Transfer t) => new(
        t.Id, t.TransferNumber, t.FromWarehouseId, t.FromWarehouse.Name, t.ToWarehouseId, t.ToWarehouse.Name,
        t.TransferDate, t.Reference, t.Notes, t.Status.ToString(),
        t.Lines.Select(l => new TransferLineDto(l.ProductId, l.Product.Sku, l.Product.Name, l.Quantity, l.Unit)).ToList(),
        t.CreatedAtUtc);
}
