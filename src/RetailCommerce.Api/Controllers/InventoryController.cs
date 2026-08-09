using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Inventory;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] InventoryListQuery query, CancellationToken ct) =>
        Ok(await inventoryService.ListAsync(query, ct));

    [HttpPost("adjust")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<InventoryBalanceDto>> Adjust(AdjustStockRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        return Ok(await inventoryService.AdjustAsync(request, userId, ct));
    }

    [HttpGet("movements")]
    public async Task<ActionResult> Movements([FromQuery] StockMovementListQuery query, CancellationToken ct) =>
        Ok(await inventoryService.ListMovementsAsync(query, ct));
}
