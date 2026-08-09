using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Customers;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] CustomerListQuery query, CancellationToken ct) =>
        Ok(await customerService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await customerService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "CustomerManagers")]
    public async Task<ActionResult<CustomerDto>> Create(UpsertCustomerRequest request, CancellationToken ct)
    {
        var created = await customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CustomerManagers")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpsertCustomerRequest request, CancellationToken ct) =>
        Ok(await customerService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CustomerManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await customerService.DeleteAsync(id, ct);
        return NoContent();
    }
}
