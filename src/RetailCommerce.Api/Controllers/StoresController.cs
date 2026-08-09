using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Stores;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoresController(IStoreService storeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] StoreListQuery query, CancellationToken ct) =>
        Ok(await storeService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoreDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await storeService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<StoreDto>> Create(UpsertStoreRequest request, CancellationToken ct)
    {
        var created = await storeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<StoreDto>> Update(Guid id, UpsertStoreRequest request, CancellationToken ct) =>
        Ok(await storeService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await storeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
