using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Auth;
using RetailCommerce.Application.Common;
using RetailCommerce.Domain.Sales;
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

        // Rotate: revoke the presented token and issue a brand new pair. Re-resolving terminal
        // context here (not just at login) is what lets an already-logged-in cashier's session
        // pick up a newly-assigned default terminal on its next silent refresh, rather than being
        // stuck with a stale back-office-shaped token until they explicitly log out/in.
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

    public async Task<AuthResponse> SelectTerminalAsync(Guid userId, Guid terminalId, string? ipAddress, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Session is no longer valid.");
        }

        var isAssigned = await db.PosTerminalUsers.AnyAsync(x => x.UserId == userId && x.TerminalId == terminalId, ct);
        if (!isAssigned)
        {
            throw new ForbiddenException("You are not assigned to that POS terminal.");
        }

        var terminal = await db.PosTerminals
            .Include(t => t.Warehouse).ThenInclude(w => w.Store)
            .FirstOrDefaultAsync(t => t.Id == terminalId, ct);
        if (terminal is null || !terminal.IsActive)
        {
            throw new ConflictException("That POS terminal is not active.");
        }

        var claims = ClaimsFor(terminal);
        var roles = await userManager.GetRolesAsync(user);
        return await MintTokensAsync(user, roles, claims, selectionRequired: false, availableTerminals: null, ipAddress, ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);

        var terminalIds = await db.PosTerminalUsers.Where(x => x.UserId == user.Id).Select(x => x.TerminalId).ToListAsync(ct);
        var assignedTerminals = terminalIds.Count == 0
            ? new List<PosTerminal>()
            : await db.PosTerminals
                .Include(t => t.Warehouse).ThenInclude(w => w.Store)
                .Where(t => terminalIds.Contains(t.Id) && t.IsActive)
                .ToListAsync(ct);

        PosTerminalClaims? claims = null;
        var selectionRequired = false;
        IReadOnlyList<TerminalOptionDto>? availableTerminals = null;

        if (assignedTerminals.Count == 1)
        {
            claims = ClaimsFor(assignedTerminals[0]);
        }
        else if (assignedTerminals.Count > 1)
        {
            selectionRequired = true;
            availableTerminals = assignedTerminals
                .Select(t => new TerminalOptionDto(t.Id, t.Code, t.Name, t.Warehouse.Store?.Name ?? t.Warehouse.Name))
                .ToList();
        }

        return await MintTokensAsync(user, roles, claims, selectionRequired, availableTerminals, ipAddress, ct);
    }

    private async Task<AuthResponse> MintTokensAsync(
        ApplicationUser user,
        IList<string> roles,
        PosTerminalClaims? claims,
        bool selectionRequired,
        IReadOnlyList<TerminalOptionDto>? availableTerminals,
        string? ipAddress,
        CancellationToken ct)
    {
        var (accessToken, accessExpires) = tokenService.GenerateAccessToken(user, roles, claims, selectionRequired);
        var (refreshToken, refreshExpires) = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.Hash(refreshToken),
            ExpiresAtUtc = refreshExpires,
            CreatedByIp = ipAddress,
        });
        await db.SaveChangesAsync(ct);

        var userDto = new CurrentUserDto(
            user.Id, user.Email!, user.FirstName, user.LastName,
            claims?.StoreId ?? user.StoreId, roles.ToList(),
            claims?.TerminalId, claims?.WarehouseId, claims?.TerminalName, claims?.StoreName);

        return new AuthResponse(accessToken, accessExpires, refreshToken, refreshExpires, userDto, availableTerminals);
    }

    private static PosTerminalClaims ClaimsFor(PosTerminal terminal)
    {
        var storeId = terminal.Warehouse.StoreId
            ?? throw new ConflictException($"POS terminal '{terminal.Code}' is not linked to a store.");
        var storeName = terminal.Warehouse.Store?.Name ?? terminal.Warehouse.Name;
        return new PosTerminalClaims(terminal.Id, terminal.WarehouseId, storeId, terminal.Name, storeName);
    }
}
