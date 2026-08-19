using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Auth;
using RetailCommerce.Application.Common;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Called after login when the user has 2+ assigned POS terminals (AuthResponse.
    /// AvailableTerminals was populated and the token is access-restricted until this runs) —
    /// requires the restricted token itself so we know who's asking.</summary>
    [HttpPost("select-terminal")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> SelectTerminal(SelectTerminalRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException("Session is no longer valid.");
        var result = await authService.SelectTerminalAsync(userId, request.TerminalId, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }
}
