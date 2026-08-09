using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Stores;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Stores;

public class StoreService(AppDbContext db) : IStoreService
{
    public async Task<PagedResult<StoreDto>> ListAsync(StoreListQuery query, CancellationToken ct = default)
    {
        var stores = db.Stores.Include(s => s.Warehouses).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            stores = stores.Where(s => EF.Functions.ILike(s.Name, $"%{term}%") || EF.Functions.ILike(s.Code, $"%{term}%"));
        }

        var totalCount = await stores.CountAsync(ct);
        var page = await stores
            .OrderBy(s => s.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<StoreDto>
        {
            Items = page.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<StoreDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var store = await db.Stores.Include(s => s.Warehouses).FirstOrDefaultAsync(s => s.Id == id, ct)
                    ?? throw new NotFoundException("Store", id);
        return ToDto(store);
    }

    public async Task<StoreDto> CreateAsync(UpsertStoreRequest request, CancellationToken ct = default)
    {
        await EnsureCodeUniqueAsync(request.Code, null, ct);
        var store = new Store { Id = Guid.NewGuid() };
        MapRequestToEntity(request, store);
        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);
        return ToDto(store);
    }

    public async Task<StoreDto> UpdateAsync(Guid id, UpsertStoreRequest request, CancellationToken ct = default)
    {
        var store = await db.Stores.Include(s => s.Warehouses).FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Store", id);
        await EnsureCodeUniqueAsync(request.Code, id, ct);
        MapRequestToEntity(request, store);
        await db.SaveChangesAsync(ct);
        return ToDto(store);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var store = await db.Stores.Include(s => s.Warehouses).FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Store", id);
        if (store.Warehouses.Count > 0)
        {
            throw new ConflictException("Cannot delete a store that still has warehouses linked to it.");
        }
        db.Stores.Remove(store);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? existingId, CancellationToken ct)
    {
        var taken = await db.Stores.AnyAsync(s => s.Code == code.Trim() && s.Id != existingId, ct);
        if (taken) throw new ConflictException($"Store code '{code}' is already in use.");
    }

    private static void MapRequestToEntity(UpsertStoreRequest r, Store s)
    {
        s.Code = r.Code.Trim();
        s.Name = r.Name.Trim();
        s.City = string.IsNullOrWhiteSpace(r.City) ? null : r.City.Trim();
        s.Address = string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim();
        s.Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        s.Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim();
        s.Ntn = string.IsNullOrWhiteSpace(r.Ntn) ? null : r.Ntn.Trim();
        s.Strn = string.IsNullOrWhiteSpace(r.Strn) ? null : r.Strn.Trim();
        s.ReceiptFooterText = string.IsNullOrWhiteSpace(r.ReceiptFooterText) ? null : r.ReceiptFooterText.Trim();
        s.Status = Enum.Parse<PartyStatus>(r.Status, true);
    }

    private static StoreDto ToDto(Store s) =>
        new(s.Id, s.Code, s.Name, s.City, s.Address, s.Phone, s.Email, s.Ntn, s.Strn, s.ReceiptFooterText, s.Status.ToString(), s.Warehouses.Count);
}
