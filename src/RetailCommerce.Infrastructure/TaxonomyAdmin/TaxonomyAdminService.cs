using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.TaxonomyAdmin;
using RetailCommerce.Domain.Taxonomy;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.TaxonomyAdmin;

public class TaxonomyAdminService(AppDbContext db) : ITaxonomyAdminService
{
    private readonly SimpleTaxonomyNodeService<Department> _departments = new(db);
    private readonly SimpleTaxonomyNodeService<Gender> _genders = new(db);
    private readonly SimpleTaxonomyNodeService<EventType> _eventTypes = new(db);

    public Task<IReadOnlyList<TaxonomyItemDto>> ListDepartmentsAsync(CancellationToken ct = default) => _departments.ListAsync(ct);
    public Task<TaxonomyItemDto> CreateDepartmentAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _departments.CreateAsync(request, ct);
    public Task<TaxonomyItemDto> UpdateDepartmentAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _departments.UpdateAsync(id, request, ct);
    public Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default) => _departments.DeleteAsync(id, ct);

    public Task<IReadOnlyList<TaxonomyItemDto>> ListGendersAsync(CancellationToken ct = default) => _genders.ListAsync(ct);
    public Task<TaxonomyItemDto> CreateGenderAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _genders.CreateAsync(request, ct);
    public Task<TaxonomyItemDto> UpdateGenderAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _genders.UpdateAsync(id, request, ct);
    public Task DeleteGenderAsync(Guid id, CancellationToken ct = default) => _genders.DeleteAsync(id, ct);

    public Task<IReadOnlyList<TaxonomyItemDto>> ListEventTypesAsync(CancellationToken ct = default) => _eventTypes.ListAsync(ct);
    public Task<TaxonomyItemDto> CreateEventTypeAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _eventTypes.CreateAsync(request, ct);
    public Task<TaxonomyItemDto> UpdateEventTypeAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default) => _eventTypes.UpdateAsync(id, request, ct);
    public Task DeleteEventTypeAsync(Guid id, CancellationToken ct = default) => _eventTypes.DeleteAsync(id, ct);

    public async Task<IReadOnlyList<CategoryItemDto>> ListCategoriesAsync(Guid? departmentId, CancellationToken ct = default)
    {
        var query = db.Categories.Include(c => c.Department).Where(c => c.IsActive);
        if (departmentId is { } dep) query = query.Where(c => c.DepartmentId == dep);
        return await query.OrderBy(c => c.Name)
            .Select(c => new CategoryItemDto(c.Id, c.Code, c.Name, c.DepartmentId, c.Department.Name))
            .ToListAsync(ct);
    }

    public async Task<CategoryItemDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken ct = default)
    {
        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
        {
            throw new NotFoundException("Department", request.DepartmentId);
        }
        await EnsureCategoryCodeUniqueAsync(request.DepartmentId, request.Code, null, ct);

        var entity = new Category { DepartmentId = request.DepartmentId, Code = request.Code.Trim(), Name = request.Name.Trim(), IsActive = true };
        db.Categories.Add(entity);
        await db.SaveChangesAsync(ct);
        return await GetCategoryDtoAsync(entity.Id, ct);
    }

    public async Task<CategoryItemDto> UpdateCategoryAsync(Guid id, UpsertCategoryRequest request, CancellationToken ct = default)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Category", id);
        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
        {
            throw new NotFoundException("Department", request.DepartmentId);
        }
        await EnsureCategoryCodeUniqueAsync(request.DepartmentId, request.Code, id, ct);

        entity.DepartmentId = request.DepartmentId;
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        return await GetCategoryDtoAsync(entity.Id, ct);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Category", id);
        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SubcategoryItemDto>> ListSubcategoriesAsync(Guid? categoryId, CancellationToken ct = default)
    {
        var query = db.Subcategories.Include(s => s.Category).Where(s => s.IsActive);
        if (categoryId is { } cat) query = query.Where(s => s.CategoryId == cat);
        return await query.OrderBy(s => s.Name)
            .Select(s => new SubcategoryItemDto(s.Id, s.Code, s.Name, s.CategoryId, s.Category.Name))
            .ToListAsync(ct);
    }

    public async Task<SubcategoryItemDto> CreateSubcategoryAsync(UpsertSubcategoryRequest request, CancellationToken ct = default)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            throw new NotFoundException("Category", request.CategoryId);
        }
        await EnsureSubcategoryCodeUniqueAsync(request.CategoryId, request.Code, null, ct);

        var entity = new Subcategory { CategoryId = request.CategoryId, Code = request.Code.Trim(), Name = request.Name.Trim(), IsActive = true };
        db.Subcategories.Add(entity);
        await db.SaveChangesAsync(ct);
        return await GetSubcategoryDtoAsync(entity.Id, ct);
    }

    public async Task<SubcategoryItemDto> UpdateSubcategoryAsync(Guid id, UpsertSubcategoryRequest request, CancellationToken ct = default)
    {
        var entity = await db.Subcategories.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Subcategory", id);
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            throw new NotFoundException("Category", request.CategoryId);
        }
        await EnsureSubcategoryCodeUniqueAsync(request.CategoryId, request.Code, id, ct);

        entity.CategoryId = request.CategoryId;
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        return await GetSubcategoryDtoAsync(entity.Id, ct);
    }

    public async Task DeleteSubcategoryAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Subcategories.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Subcategory", id);
        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CollectionItemDto>> ListCollectionsAsync(CancellationToken ct = default)
    {
        var entities = await db.Collections
            .OrderByDescending(c => c.Year).ThenBy(c => c.Name)
            .ToListAsync(ct);
        return entities
            .Select(c => new CollectionItemDto(c.Id, c.Name, c.VersionLabel, c.Year, c.DisplayCode, c.DepartmentId))
            .ToList();
    }

    public async Task<CollectionItemDto> CreateCollectionAsync(UpsertCollectionRequest request, CancellationToken ct = default)
    {
        if (request.DepartmentId is { } dep && !await db.Departments.AnyAsync(d => d.Id == dep, ct))
        {
            throw new NotFoundException("Department", dep);
        }

        var entity = new Collection
        {
            Name = request.Name.Trim(),
            VersionLabel = request.VersionLabel.Trim(),
            Year = request.Year,
            DepartmentId = request.DepartmentId,
        };
        db.Collections.Add(entity);
        await db.SaveChangesAsync(ct);
        return new CollectionItemDto(entity.Id, entity.Name, entity.VersionLabel, entity.Year, entity.DisplayCode, entity.DepartmentId);
    }

    public async Task<CollectionItemDto> UpdateCollectionAsync(Guid id, UpsertCollectionRequest request, CancellationToken ct = default)
    {
        var entity = await db.Collections.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Collection", id);
        if (request.DepartmentId is { } dep && !await db.Departments.AnyAsync(d => d.Id == dep, ct))
        {
            throw new NotFoundException("Department", dep);
        }

        entity.Name = request.Name.Trim();
        entity.VersionLabel = request.VersionLabel.Trim();
        entity.Year = request.Year;
        entity.DepartmentId = request.DepartmentId;
        await db.SaveChangesAsync(ct);
        return new CollectionItemDto(entity.Id, entity.Name, entity.VersionLabel, entity.Year, entity.DisplayCode, entity.DepartmentId);
    }

    public async Task DeleteCollectionAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Collections.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Collection", id);
        db.Collections.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCategoryCodeUniqueAsync(Guid departmentId, string code, Guid? existingId, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var taken = await db.Categories.AnyAsync(c => c.DepartmentId == departmentId && c.Code == trimmed && c.Id != existingId, ct);
        if (taken) throw new ConflictException($"Category code '{code}' already exists in this department.");
    }

    private async Task EnsureSubcategoryCodeUniqueAsync(Guid categoryId, string code, Guid? existingId, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var taken = await db.Subcategories.AnyAsync(s => s.CategoryId == categoryId && s.Code == trimmed && s.Id != existingId, ct);
        if (taken) throw new ConflictException($"Subcategory code '{code}' already exists in this category.");
    }

    private async Task<CategoryItemDto> GetCategoryDtoAsync(Guid id, CancellationToken ct) =>
        await db.Categories.Include(c => c.Department)
            .Where(c => c.Id == id)
            .Select(c => new CategoryItemDto(c.Id, c.Code, c.Name, c.DepartmentId, c.Department.Name))
            .SingleAsync(ct);

    private async Task<SubcategoryItemDto> GetSubcategoryDtoAsync(Guid id, CancellationToken ct) =>
        await db.Subcategories.Include(s => s.Category)
            .Where(s => s.Id == id)
            .Select(s => new SubcategoryItemDto(s.Id, s.Code, s.Name, s.CategoryId, s.Category.Name))
            .SingleAsync(ct);
}
