using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.TaxonomyAdmin;
using RetailCommerce.Domain.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.TaxonomyAdmin;

/// <summary>Shared CRUD for the three flat taxonomy lookups (Department, Gender, EventType) —
/// they're identical in shape (Code/Name/IsActive), so one generic implementation replaces
/// three near-duplicate services.</summary>
public class SimpleTaxonomyNodeService<TEntity>(AppDbContext db) where TEntity : BaseEntity, ITaxonomyNode, new()
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> ListAsync(CancellationToken ct = default) =>
        await db.Set<TEntity>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new TaxonomyItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

    public async Task<TaxonomyItemDto> CreateAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default)
    {
        await EnsureCodeUniqueAsync(request.Code, null, ct);
        var entity = new TEntity { Code = request.Code.Trim(), Name = request.Name.Trim(), IsActive = true };
        db.Set<TEntity>().Add(entity);
        await db.SaveChangesAsync(ct);
        return new TaxonomyItemDto(entity.Id, entity.Code, entity.Name);
    }

    public async Task<TaxonomyItemDto> UpdateAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default)
    {
        var entity = await db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new NotFoundException(typeof(TEntity).Name, id);
        await EnsureCodeUniqueAsync(request.Code, id, ct);
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        return new TaxonomyItemDto(entity.Id, entity.Code, entity.Name);
    }

    /// <summary>Soft delete only — hard-deleting would violate the Restrict FK on any Product
    /// that already references this row.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new NotFoundException(typeof(TEntity).Name, id);
        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? existingId, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var taken = await db.Set<TEntity>().AnyAsync(x => x.Code == trimmed && x.Id != existingId, ct);
        if (taken) throw new ConflictException($"Code '{code}' is already in use.");
    }
}
