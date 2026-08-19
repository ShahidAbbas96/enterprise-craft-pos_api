using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.PosTerminals;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/pos-terminals")]
[Authorize]
public class PosTerminalsController(IPosTerminalService posTerminalService) : ControllerBase
{
    // Any authenticated role can list/read — Reports' Terminal filter and other read-only UI
    // need this without being a user-manager; only mutation is restricted below.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PosTerminalDto>>> List([FromQuery] Guid? storeId, CancellationToken ct) =>
        Ok(await posTerminalService.ListAsync(storeId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PosTerminalDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await posTerminalService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "UserManagers")]
    public async Task<ActionResult<PosTerminalDto>> Create(UpsertPosTerminalRequest request, CancellationToken ct) =>
        Ok(await posTerminalService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "UserManagers")]
    public async Task<ActionResult<PosTerminalDto>> Update(Guid id, UpsertPosTerminalRequest request, CancellationToken ct) =>
        Ok(await posTerminalService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "UserManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await posTerminalService.DeleteAsync(id, ct);
        return NoContent();
    }
}
