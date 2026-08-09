using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Sales;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController(ISalesService salesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] SaleListQuery query, CancellationToken ct) =>
        Ok(await salesService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await salesService.GetAsync(id, ct));

    /// <summary>The real POS "Pay" endpoint — validates stock, deducts inventory, records
    /// payment, awards loyalty points, and issues a server-generated invoice number.</summary>
    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create(CreateSaleRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? cashierUserId = Guid.TryParse(userIdClaim, out var id) ? id : null;

        var created = await salesService.CreateSaleAsync(request, cashierUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}
