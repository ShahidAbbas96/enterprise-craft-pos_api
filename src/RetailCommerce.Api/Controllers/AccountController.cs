using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Account;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken ct) =>
        Ok(await accountService.GetProfileAsync(CurrentUserId, ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await accountService.ChangePasswordAsync(CurrentUserId, request, ct);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
