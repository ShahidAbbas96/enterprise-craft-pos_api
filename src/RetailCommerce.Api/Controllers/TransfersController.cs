using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Purchasing;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public class TransfersController(ITransferService transferService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] TransferListQuery query, CancellationToken ct) =>
        Ok(await transferService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransferDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await transferService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TransferDto>> Create(CreateTransferRequest request, CancellationToken ct)
    {
        var created = await transferService.CreateAsync(request, CurrentUserId(), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TransferDto>> Complete(Guid id, CancellationToken ct) =>
        Ok(await transferService.CompleteAsync(id, CurrentUserId(), ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<TransferDto>> Cancel(Guid id, CancellationToken ct) =>
        Ok(await transferService.CancelAsync(id, ct));

    private Guid? CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
