using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Warehouses;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Warehouses;

public class WarehouseService(AppDbContext db) : IWarehouseService
{
    public async Task<PagedResult<WarehouseDto>> ListAsync(WarehouseListQuery query, CancellationToken ct = default)
    {
        var warehouses = db.Warehouses.Include(w => w.Store).AsQueryable();

        if (query.StoreId is { } storeId) warehouses = warehouses.Where(w => w.StoreId == storeId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            warehouses = warehouses.Where(w => EF.Functions.ILike(w.Name, $"%{term}%") || EF.Functions.ILike(w.Code, $"%{term}%"));
        }

        var totalCount = await warehouses.CountAsync(ct);
        var page = await warehouses
            .OrderBy(w => w.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<WarehouseDto>
        {
            Items = page.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<WarehouseDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var warehouse = await db.Warehouses.Include(w => w.Store).FirstOrDefaultAsync(w => w.Id == id, ct)
                         ?? throw new NotFoundException("Warehouse", id);
        return ToDto(warehouse);
    }

    public async Task<WarehouseDto> CreateAsync(UpsertWarehouseRequest request, CancellationToken ct = default)
    {
        await EnsureCodeUniqueAsync(request.Code, null, ct);
        await EnsureStoreExistsAsync(request.StoreId, ct);

        var warehouse = new Warehouse { Id = Guid.NewGuid() };
        MapRequestToEntity(request, warehouse);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return await GetAsync(warehouse.Id, ct);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpsertWarehouseRequest request, CancellationToken ct = default)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw new NotFoundException("Warehouse", id);
        await EnsureCodeUniqueAsync(request.Code, id, ct);
        await EnsureStoreExistsAsync(request.StoreId, ct);
        MapRequestToEntity(request, warehouse);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw new NotFoundException("Warehouse", id);
        var hasStock = await db.InventoryBalances.AnyAsync(i => i.WarehouseId == id && i.Quantity != 0, ct);
        if (hasStock) throw new ConflictException("Cannot delete a warehouse that still has stock on hand.");
        db.Warehouses.Remove(warehouse);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? existingId, CancellationToken ct)
    {
        var taken = await db.Warehouses.AnyAsync(w => w.Code == code.Trim() && w.Id != existingId, ct);
        if (taken) throw new ConflictException($"Warehouse code '{code}' is already in use.");
    }

    private async Task EnsureStoreExistsAsync(Guid? storeId, CancellationToken ct)
    {
        if (storeId is { } id && !await db.Stores.AnyAsync(s => s.Id == id, ct))
        {
            throw new NotFoundException("Store", id);
        }
    }

    private static void MapRequestToEntity(UpsertWarehouseRequest r, Warehouse w)
    {
        w.Code = r.Code.Trim();
        w.Name = r.Name.Trim();
        w.City = string.IsNullOrWhiteSpace(r.City) ? null : r.City.Trim();
        w.Status = Enum.Parse<PartyStatus>(r.Status, true);
        w.StoreId = r.StoreId;
    }

    private static WarehouseDto ToDto(Warehouse w) =>
        new(w.Id, w.Code, w.Name, w.City, w.Status.ToString(), w.StoreId, w.Store?.Name);
}
