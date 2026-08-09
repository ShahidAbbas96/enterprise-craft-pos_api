using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Suppliers;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] SupplierListQuery query, CancellationToken ct) =>
        Ok(await supplierService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await supplierService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<SupplierDto>> Create(UpsertSupplierRequest request, CancellationToken ct)
    {
        var created = await supplierService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpsertSupplierRequest request, CancellationToken ct) =>
        Ok(await supplierService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await supplierService.DeleteAsync(id, ct);
        return NoContent();
    }
}
