using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Shifts;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController(IShiftService shiftService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] ShiftListQuery query, CancellationToken ct) =>
        Ok(await shiftService.ListAsync(query, ct));

    [HttpGet("open")]
    public async Task<ActionResult<ShiftDto?>> GetOpen([FromQuery] Guid warehouseId, CancellationToken ct) =>
        Ok(await shiftService.GetOpenShiftAsync(warehouseId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShiftDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await shiftService.GetAsync(id, ct));

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<ShiftSummaryDto>> Summary(Guid id, CancellationToken ct) =>
        Ok(await shiftService.GetSummaryAsync(id, ct));

    [HttpPost("open")]
    public async Task<ActionResult<ShiftDto>> Open(OpenShiftRequest request, CancellationToken ct) =>
        Ok(await shiftService.OpenShiftAsync(request, CurrentUserId, ct));

    [HttpPost("{id:guid}/expenses")]
    public async Task<ActionResult<ExpenseDto>> AddExpense(Guid id, AddExpenseRequest request, CancellationToken ct) =>
        Ok(await shiftService.AddExpenseAsync(id, request, CurrentUserId, ct));

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ShiftDto>> Close(Guid id, CloseShiftRequest request, CancellationToken ct) =>
        Ok(await shiftService.CloseShiftAsync(id, request, CurrentUserId, ct));

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
