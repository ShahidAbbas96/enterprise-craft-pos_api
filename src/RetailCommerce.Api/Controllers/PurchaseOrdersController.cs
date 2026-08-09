using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Purchasing;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController(IPurchaseOrderService purchaseOrderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] PurchaseOrderListQuery query, CancellationToken ct) =>
        Ok(await purchaseOrderService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await purchaseOrderService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var created = await purchaseOrderService.CreateAsync(request, CurrentUserId(), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<PurchaseOrderDto>> UpdateStatus(Guid id, UpdatePurchaseOrderStatusRequest request, CancellationToken ct) =>
        Ok(await purchaseOrderService.UpdateStatusAsync(id, request.Status, ct));

    [HttpPost("{id:guid}/receive")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(Guid id, CancellationToken ct) =>
        Ok(await purchaseOrderService.ReceiveAsync(id, CurrentUserId(), ct));

    private Guid? CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
