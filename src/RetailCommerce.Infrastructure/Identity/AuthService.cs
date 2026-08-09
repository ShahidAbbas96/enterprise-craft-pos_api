using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Auth;
using RetailCommerce.Application.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    AppDbContext db) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var passwordOk = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordOk)
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var hash = tokenService.Hash(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive)
        {
            throw new AuthenticationFailedException("Refresh token is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Refresh token is invalid or expired.");
        }

        // Rotate: revoke the presented token and issue a brand new pair.
        stored.RevokedAtUtc = DateTimeOffset.UtcNow;

        var response = await IssueTokensAsync(user, ipAddress, ct);
        stored.ReplacedByTokenHash = tokenService.Hash(response.RefreshToken);
        await db.SaveChangesAsync(ct);

        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokenService.Hash(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (stored is not null && stored.IsActive)
        {
            stored.RevokedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpires) = tokenService.GenerateAccessToken(user, roles);
        var (refreshToken, refreshExpires) = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.Hash(refreshToken),
            ExpiresAtUtc = refreshExpires,
            CreatedByIp = ipAddress,
        });
        await db.SaveChangesAsync(ct);

        var userDto = new CurrentUserDto(user.Id, user.Email!, user.FirstName, user.LastName, user.StoreId, roles.ToList());
        return new AuthResponse(accessToken, accessExpires, refreshToken, refreshExpires, userDto);
    }
}
