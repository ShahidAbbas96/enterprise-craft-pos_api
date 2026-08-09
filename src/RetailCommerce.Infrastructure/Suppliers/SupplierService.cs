using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Suppliers;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Parties;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Suppliers;

public class SupplierService(AppDbContext db) : ISupplierService
{
    public async Task<PagedResult<SupplierDto>> ListAsync(SupplierListQuery query, CancellationToken ct = default)
    {
        var suppliers = db.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PartyStatus>(query.Status, true, out var status))
        {
            suppliers = suppliers.Where(s => s.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            suppliers = suppliers.Where(s => EF.Functions.ILike(s.Name, $"%{term}%"));
        }

        var totalCount = await suppliers.CountAsync(ct);
        var page = await suppliers
            .OrderBy(s => s.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<SupplierDto>
        {
            Items = page.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<SupplierDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Supplier", id);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(UpsertSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = new Supplier { Id = Guid.NewGuid() };
        MapRequestToEntity(request, supplier);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpsertSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Supplier", id);
        MapRequestToEntity(request, supplier);
        await db.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Supplier", id);
        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync(ct);
    }

    private static void MapRequestToEntity(UpsertSupplierRequest r, Supplier s)
    {
        s.Name = r.Name.Trim();
        s.ContactName = string.IsNullOrWhiteSpace(r.ContactName) ? null : r.ContactName.Trim();
        s.Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim();
        s.Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        s.Rating = r.Rating;
        s.LeadDays = r.LeadDays;
        s.Status = Enum.Parse<PartyStatus>(r.Status, true);
    }

    private static SupplierDto ToDto(Supplier s) =>
        new(s.Id, s.Name, s.ContactName, s.Email, s.Phone, s.Rating, s.Balance, s.LeadDays, s.Status.ToString(), s.CreatedAtUtc);
}
