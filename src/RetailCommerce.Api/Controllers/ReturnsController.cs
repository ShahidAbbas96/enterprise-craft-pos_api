using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Returns;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/returns")]
[Authorize]
public class ReturnsController(IReturnsService returnsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] ReturnListQuery query, CancellationToken ct) =>
        Ok(await returnsService.ListAsync(query, ct));

    [HttpGet("lookup")]
    public async Task<ActionResult<ReturnableSaleDto>> Lookup([FromQuery] string orderNumber, CancellationToken ct) =>
        Ok(await returnsService.LookupAsync(orderNumber, ct));

    [HttpPost]
    public async Task<ActionResult<ReturnDto>> Create(CreateReturnRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        return Ok(await returnsService.CreateReturnAsync(request, userId, ct));
    }
}
