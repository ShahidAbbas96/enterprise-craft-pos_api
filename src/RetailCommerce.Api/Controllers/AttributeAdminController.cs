using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.AttributeAdmin;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/attribute-admin")]
[Authorize]
public class AttributeAdminController(IAttributeAdminService service) : ControllerBase
{
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<AttributeTypeItemDto>>> ListTypes(CancellationToken ct) =>
        Ok(await service.ListTypesAsync(ct));

    [HttpPost("types")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<AttributeTypeItemDto>> CreateType(UpsertAttributeTypeRequest request, CancellationToken ct) =>
        Ok(await service.CreateTypeAsync(request, ct));

    [HttpPut("types/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<AttributeTypeItemDto>> UpdateType(Guid id, UpsertAttributeTypeRequest request, CancellationToken ct) =>
        Ok(await service.UpdateTypeAsync(id, request, ct));

    [HttpDelete("types/{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteType(Guid id, CancellationToken ct)
    {
        await service.DeleteTypeAsync(id, ct);
        return NoContent();
    }

    [HttpGet("types/{typeId:guid}/options")]
    public async Task<ActionResult<IReadOnlyList<AttributeOptionItemDto>>> ListOptions(Guid typeId, CancellationToken ct) =>
        Ok(await service.ListOptionsAsync(typeId, ct));

    [HttpPost("types/{typeId:guid}/options")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<AttributeOptionItemDto>> CreateOption(Guid typeId, UpsertAttributeOptionRequest request, CancellationToken ct) =>
        Ok(await service.CreateOptionAsync(typeId, request, ct));

    [HttpPut("types/{typeId:guid}/options/{optionId:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<AttributeOptionItemDto>> UpdateOption(Guid typeId, Guid optionId, UpsertAttributeOptionRequest request, CancellationToken ct) =>
        Ok(await service.UpdateOptionAsync(typeId, optionId, request, ct));

    [HttpDelete("types/{typeId:guid}/options/{optionId:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> DeleteOption(Guid typeId, Guid optionId, CancellationToken ct)
    {
        await service.DeleteOptionAsync(typeId, optionId, ct);
        return NoContent();
    }
}
