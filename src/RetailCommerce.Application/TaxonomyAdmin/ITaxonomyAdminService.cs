namespace RetailCommerce.Application.TaxonomyAdmin;

/// <summary>Full CRUD for the taxonomy that was previously only seedable via code — without
/// this, the client could never add e.g. a MEN gender or an APPAREL department without a
/// developer touching DbSeeder.cs. Deletes are soft (IsActive=false) so existing products that
/// reference a retired taxonomy entry never break.</summary>
public interface ITaxonomyAdminService
{
    Task<IReadOnlyList<TaxonomyItemDto>> ListDepartmentsAsync(CancellationToken ct = default);
    Task<TaxonomyItemDto> CreateDepartmentAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task<TaxonomyItemDto> UpdateDepartmentAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TaxonomyItemDto>> ListGendersAsync(CancellationToken ct = default);
    Task<TaxonomyItemDto> CreateGenderAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task<TaxonomyItemDto> UpdateGenderAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task DeleteGenderAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TaxonomyItemDto>> ListEventTypesAsync(CancellationToken ct = default);
    Task<TaxonomyItemDto> CreateEventTypeAsync(UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task<TaxonomyItemDto> UpdateEventTypeAsync(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct = default);
    Task DeleteEventTypeAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryItemDto>> ListCategoriesAsync(Guid? departmentId, CancellationToken ct = default);
    Task<CategoryItemDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken ct = default);
    Task<CategoryItemDto> UpdateCategoryAsync(Guid id, UpsertCategoryRequest request, CancellationToken ct = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<SubcategoryItemDto>> ListSubcategoriesAsync(Guid? categoryId, CancellationToken ct = default);
    Task<SubcategoryItemDto> CreateSubcategoryAsync(UpsertSubcategoryRequest request, CancellationToken ct = default);
    Task<SubcategoryItemDto> UpdateSubcategoryAsync(Guid id, UpsertSubcategoryRequest request, CancellationToken ct = default);
    Task DeleteSubcategoryAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CollectionItemDto>> ListCollectionsAsync(CancellationToken ct = default);
    Task<CollectionItemDto> CreateCollectionAsync(UpsertCollectionRequest request, CancellationToken ct = default);
    Task<CollectionItemDto> UpdateCollectionAsync(Guid id, UpsertCollectionRequest request, CancellationToken ct = default);
    Task DeleteCollectionAsync(Guid id, CancellationToken ct = default);
}
