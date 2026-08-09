using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Taxonomy;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Taxonomy;

public class TaxonomyService(AppDbContext db) : ITaxonomyService
{
    public async Task<TaxonomySnapshotDto> GetSnapshotAsync(CancellationToken ct = default)
    {
        var departments = await db.Departments.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var genders = await db.Genders.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var eventTypes = await db.EventTypes.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var categories = await db.Categories.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Code, x.Name, x.DepartmentId))
            .ToListAsync(ct);

        var subcategories = await db.Subcategories.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SubcategoryDto(x.Id, x.Code, x.Name, x.CategoryId))
            .ToListAsync(ct);

        var collections = await db.Collections
            .OrderByDescending(x => x.Year).ThenBy(x => x.Name)
            .Select(x => new CollectionDto(x.Id, x.Name, x.VersionLabel, x.Year, x.Name + "-" + x.VersionLabel + "-" + (x.Year % 100), x.DepartmentId))
            .ToListAsync(ct);

        var attributeTypes = await db.ProductAttributeTypes
            .Include(x => x.Options)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync(ct);

        var attributeTypeDtos = attributeTypes
            .Select(t => new AttributeTypeDto(
                t.Id, t.Code, t.Name, t.DisplayOrder, t.IsRequired, t.DepartmentId,
                t.Options.Where(o => o.IsActive)
                    .OrderBy(o => o.Name)
                    .Select(o => new AttributeOptionDto(o.Id, o.Code, o.Name))
                    .ToList()))
            .ToList();

        return new TaxonomySnapshotDto(departments, genders, eventTypes, categories, subcategories, collections, attributeTypeDtos);
    }
}
