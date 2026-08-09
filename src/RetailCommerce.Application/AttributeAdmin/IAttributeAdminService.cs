namespace RetailCommerce.Application.AttributeAdmin;

/// <summary>Settings-managed CRUD for the department-aware product attribute dimensions (e.g.
/// "Color", "Size", "Upper Material") and each dimension's option list — previously only
/// seedable via code (DbSeeder). Adding a type here makes it immediately show up as a dropdown
/// on the Add/Edit Product form (see TaxonomyService.GetSnapshotAsync) with zero frontend
/// changes needed, since that form already renders one dropdown per active attribute type.</summary>
public interface IAttributeAdminService
{
    Task<IReadOnlyList<AttributeTypeItemDto>> ListTypesAsync(CancellationToken ct = default);
    Task<AttributeTypeItemDto> CreateTypeAsync(UpsertAttributeTypeRequest request, CancellationToken ct = default);
    Task<AttributeTypeItemDto> UpdateTypeAsync(Guid id, UpsertAttributeTypeRequest request, CancellationToken ct = default);
    Task DeleteTypeAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AttributeOptionItemDto>> ListOptionsAsync(Guid typeId, CancellationToken ct = default);
    Task<AttributeOptionItemDto> CreateOptionAsync(Guid typeId, UpsertAttributeOptionRequest request, CancellationToken ct = default);
    Task<AttributeOptionItemDto> UpdateOptionAsync(Guid typeId, Guid optionId, UpsertAttributeOptionRequest request, CancellationToken ct = default);
    Task DeleteOptionAsync(Guid typeId, Guid optionId, CancellationToken ct = default);
}
