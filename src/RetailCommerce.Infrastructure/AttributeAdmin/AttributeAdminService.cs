using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.AttributeAdmin;
using RetailCommerce.Application.Common;
using RetailCommerce.Domain.Taxonomy;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.AttributeAdmin;

public class AttributeAdminService(AppDbContext db) : IAttributeAdminService
{
    public async Task<IReadOnlyList<AttributeTypeItemDto>> ListTypesAsync(CancellationToken ct = default)
    {
        var entities = await db.ProductAttributeTypes
            .Include(t => t.Department)
            .Include(t => t.Options)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .ToListAsync(ct);
        return entities.Select(ToTypeDto).ToList();
    }

    public async Task<AttributeTypeItemDto> CreateTypeAsync(UpsertAttributeTypeRequest request, CancellationToken ct = default)
    {
        await EnsureCodeUniqueAsync(request.Code, null, ct);
        if (request.DepartmentId is { } deptId && !await db.Departments.AnyAsync(d => d.Id == deptId, ct))
        {
            throw new NotFoundException("Department", deptId);
        }

        var entity = new ProductAttributeType
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsRequired = request.IsRequired,
            ShowOnPurchaseOrder = request.ShowOnPurchaseOrder,
            DepartmentId = request.DepartmentId,
        };
        db.ProductAttributeTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return await GetTypeDtoAsync(entity.Id, ct);
    }

    public async Task<AttributeTypeItemDto> UpdateTypeAsync(Guid id, UpsertAttributeTypeRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductAttributeTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
                     ?? throw new NotFoundException("ProductAttributeType", id);
        await EnsureCodeUniqueAsync(request.Code, id, ct);
        if (request.DepartmentId is { } deptId && !await db.Departments.AnyAsync(d => d.Id == deptId, ct))
        {
            throw new NotFoundException("Department", deptId);
        }

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsRequired = request.IsRequired;
        entity.ShowOnPurchaseOrder = request.ShowOnPurchaseOrder;
        entity.DepartmentId = request.DepartmentId;
        await db.SaveChangesAsync(ct);
        return await GetTypeDtoAsync(entity.Id, ct);
    }

    public async Task DeleteTypeAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.ProductAttributeTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
                     ?? throw new NotFoundException("ProductAttributeType", id);
        if (await db.ProductAttributeValues.AnyAsync(v => v.ProductAttributeTypeId == id, ct))
        {
            throw new ConflictException("Cannot delete an attribute type that's already assigned to products. Retire its options instead, or reassign those products first.");
        }
        db.ProductAttributeTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AttributeOptionItemDto>> ListOptionsAsync(Guid typeId, CancellationToken ct = default)
    {
        if (!await db.ProductAttributeTypes.AnyAsync(t => t.Id == typeId, ct))
        {
            throw new NotFoundException("ProductAttributeType", typeId);
        }
        return await db.ProductAttributeOptions
            .Where(o => o.ProductAttributeTypeId == typeId)
            .OrderBy(o => o.Name)
            .Select(o => new AttributeOptionItemDto(o.Id, o.ProductAttributeTypeId, o.Code, o.Name, o.IsActive, o.BarcodeCode))
            .ToListAsync(ct);
    }

    public async Task<AttributeOptionItemDto> CreateOptionAsync(Guid typeId, UpsertAttributeOptionRequest request, CancellationToken ct = default)
    {
        if (!await db.ProductAttributeTypes.AnyAsync(t => t.Id == typeId, ct))
        {
            throw new NotFoundException("ProductAttributeType", typeId);
        }
        await EnsureOptionCodeUniqueAsync(typeId, request.Code, null, ct);

        var entity = new ProductAttributeOption
        {
            ProductAttributeTypeId = typeId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            BarcodeCode = string.IsNullOrWhiteSpace(request.BarcodeCode) ? null : request.BarcodeCode.Trim().ToUpperInvariant(),
        };
        db.ProductAttributeOptions.Add(entity);
        await db.SaveChangesAsync(ct);
        return new AttributeOptionItemDto(entity.Id, entity.ProductAttributeTypeId, entity.Code, entity.Name, entity.IsActive, entity.BarcodeCode);
    }

    public async Task<AttributeOptionItemDto> UpdateOptionAsync(Guid typeId, Guid optionId, UpsertAttributeOptionRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductAttributeOptions.FirstOrDefaultAsync(o => o.Id == optionId && o.ProductAttributeTypeId == typeId, ct)
                     ?? throw new NotFoundException("ProductAttributeOption", optionId);
        await EnsureOptionCodeUniqueAsync(typeId, request.Code, optionId, ct);

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
        entity.BarcodeCode = string.IsNullOrWhiteSpace(request.BarcodeCode) ? null : request.BarcodeCode.Trim().ToUpperInvariant();
        await db.SaveChangesAsync(ct);
        return new AttributeOptionItemDto(entity.Id, entity.ProductAttributeTypeId, entity.Code, entity.Name, entity.IsActive, entity.BarcodeCode);
    }

    public async Task DeleteOptionAsync(Guid typeId, Guid optionId, CancellationToken ct = default)
    {
        var entity = await db.ProductAttributeOptions.FirstOrDefaultAsync(o => o.Id == optionId && o.ProductAttributeTypeId == typeId, ct)
                     ?? throw new NotFoundException("ProductAttributeOption", optionId);
        // Soft delete — hard-deleting would violate the Restrict FK on any product already
        // carrying this option as a ProductAttributeValue.
        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? existingId, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var taken = await db.ProductAttributeTypes.AnyAsync(t => t.Code == trimmed && t.Id != existingId, ct);
        if (taken) throw new ConflictException($"Attribute type code '{code}' is already in use.");
    }

    private async Task EnsureOptionCodeUniqueAsync(Guid typeId, string code, Guid? existingId, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var taken = await db.ProductAttributeOptions.AnyAsync(o => o.ProductAttributeTypeId == typeId && o.Code == trimmed && o.Id != existingId, ct);
        if (taken) throw new ConflictException($"Option code '{code}' already exists for this attribute type.");
    }

    private async Task<AttributeTypeItemDto> GetTypeDtoAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ProductAttributeTypes
            .Include(t => t.Department)
            .Include(t => t.Options)
            .FirstAsync(t => t.Id == id, ct);
        return ToTypeDto(entity);
    }

    private static AttributeTypeItemDto ToTypeDto(ProductAttributeType t) => new(
        t.Id, t.Code, t.Name, t.DisplayOrder, t.IsRequired, t.ShowOnPurchaseOrder, t.DepartmentId, t.Department?.Name, t.Options.Count);
}
