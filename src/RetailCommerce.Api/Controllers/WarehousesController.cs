using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Warehouses;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] WarehouseListQuery query, CancellationToken ct) =>
        Ok(await warehouseService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await warehouseService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<WarehouseDto>> Create(UpsertWarehouseRequest request, CancellationToken ct)
    {
        var created = await warehouseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<WarehouseDto>> Update(Guid id, UpsertWarehouseRequest request, CancellationToken ct) =>
        Ok(await warehouseService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await warehouseService.DeleteAsync(id, ct);
        return NoContent();
    }
}
