using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Discounts;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/discounts")]
[Authorize]
public class DiscountsController(IDiscountService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DiscountDto>>> List([FromQuery] bool activeOnly, CancellationToken ct) =>
        Ok(await service.ListAsync(activeOnly, ct: ct));

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<DiscountDto>> Create(UpsertDiscountRequest request, CancellationToken ct) =>
        Ok(await service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<DiscountDto>> Update(Guid id, UpsertDiscountRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
