using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.TaxonomyAdmin;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/taxonomy-admin")]
[Authorize]
public class TaxonomyAdminController(ITaxonomyAdminService service) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<TaxonomyItemDto>>> ListDepartments(CancellationToken ct) =>
        Ok(await service.ListDepartmentsAsync(ct));

    [HttpPost("departments")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TaxonomyItemDto>> CreateDepartment(UpsertTaxonomyItemRequest request, CancellationToken ct) =>
        Ok(await service.CreateDepartmentAsync(request, ct));

    [HttpPut("departments/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TaxonomyItemDto>> UpdateDepartment(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct) =>
        Ok(await service.UpdateDepartmentAsync(id, request, ct));

    [HttpDelete("departments/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken ct)
    {
        await service.DeleteDepartmentAsync(id, ct);
        return NoContent();
    }

    [HttpGet("genders")]
    public async Task<ActionResult<IReadOnlyList<TaxonomyItemDto>>> ListGenders(CancellationToken ct) =>
        Ok(await service.ListGendersAsync(ct));

    [HttpPost("genders")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TaxonomyItemDto>> CreateGender(UpsertTaxonomyItemRequest request, CancellationToken ct) =>
        Ok(await service.CreateGenderAsync(request, ct));

    [HttpPut("genders/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TaxonomyItemDto>> UpdateGender(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct) =>
        Ok(await service.UpdateGenderAsync(id, request, ct));

    [HttpDelete("genders/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteGender(Guid id, CancellationToken ct)
    {
        await service.DeleteGenderAsync(id, ct);
        return NoContent();
    }

    [HttpGet("event-types")]
    public async Task<ActionResult<IReadOnlyList<TaxonomyItemDto>>> ListEventTypes(CancellationToken ct) =>
        Ok(await service.ListEventTypesAsync(ct));

    [HttpPost("event-types")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TaxonomyItemDto>> CreateEventType(UpsertTaxonomyItemRequest request, CancellationToken ct) =>
        Ok(await service.CreateEventTypeAsync(request, ct));

    [HttpPut("event-types/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TaxonomyItemDto>> UpdateEventType(Guid id, UpsertTaxonomyItemRequest request, CancellationToken ct) =>
        Ok(await service.UpdateEventTypeAsync(id, request, ct));

    [HttpDelete("event-types/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteEventType(Guid id, CancellationToken ct)
    {
        await service.DeleteEventTypeAsync(id, ct);
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryItemDto>>> ListCategories([FromQuery] Guid? departmentId, CancellationToken ct) =>
        Ok(await service.ListCategoriesAsync(departmentId, ct));

    [HttpPost("categories")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<CategoryItemDto>> CreateCategory(UpsertCategoryRequest request, CancellationToken ct) =>
        Ok(await service.CreateCategoryAsync(request, ct));

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<CategoryItemDto>> UpdateCategory(Guid id, UpsertCategoryRequest request, CancellationToken ct) =>
        Ok(await service.UpdateCategoryAsync(id, request, ct));

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        await service.DeleteCategoryAsync(id, ct);
        return NoContent();
    }

    [HttpGet("subcategories")]
    public async Task<ActionResult<IReadOnlyList<SubcategoryItemDto>>> ListSubcategories([FromQuery] Guid? categoryId, CancellationToken ct) =>
        Ok(await service.ListSubcategoriesAsync(categoryId, ct));

    [HttpPost("subcategories")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<SubcategoryItemDto>> CreateSubcategory(UpsertSubcategoryRequest request, CancellationToken ct) =>
        Ok(await service.CreateSubcategoryAsync(request, ct));

    [HttpPut("subcategories/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<SubcategoryItemDto>> UpdateSubcategory(Guid id, UpsertSubcategoryRequest request, CancellationToken ct) =>
        Ok(await service.UpdateSubcategoryAsync(id, request, ct));

    [HttpDelete("subcategories/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteSubcategory(Guid id, CancellationToken ct)
    {
        await service.DeleteSubcategoryAsync(id, ct);
        return NoContent();
    }

    [HttpGet("collections")]
    public async Task<ActionResult<IReadOnlyList<CollectionItemDto>>> ListCollections(CancellationToken ct) =>
        Ok(await service.ListCollectionsAsync(ct));

    [HttpPost("collections")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<CollectionItemDto>> CreateCollection(UpsertCollectionRequest request, CancellationToken ct) =>
        Ok(await service.CreateCollectionAsync(request, ct));

    [HttpPut("collections/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<CollectionItemDto>> UpdateCollection(Guid id, UpsertCollectionRequest request, CancellationToken ct) =>
        Ok(await service.UpdateCollectionAsync(id, request, ct));

    [HttpDelete("collections/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken ct)
    {
        await service.DeleteCollectionAsync(id, ct);
        return NoContent();
    }
}
