using System.Security.Claims;

namespace RetailCommerce.Infrastructure.Identity;

/// <summary>The resolved POS-terminal context to bake into an access token as claims, minted
/// once a user's assigned terminal is known (either automatically, when they have exactly one,
/// or explicitly via POST /api/auth/select-terminal).</summary>
public record PosTerminalClaims(Guid TerminalId, Guid WarehouseId, Guid StoreId, string TerminalName, string StoreName);

public interface ITokenService
{
    /// <summary>
    /// <paramref name="terminal"/>: when set, the token carries terminal_id/warehouse_id/store_id
    /// claims scoping every POS-runtime request to that single terminal — see
    /// ICurrentUserService.ResolveWarehouseScope.
    /// <paramref name="terminalSelectionRequired"/>: set only when the user has 2+ assigned
    /// terminals and hasn't picked one yet — the token carries a terminal_required claim that a
    /// startup middleware uses to block every route except /api/auth/* until they do.
    /// </summary>
    (string token, DateTimeOffset expiresAtUtc) GenerateAccessToken(
        ApplicationUser user,
        IList<string> roles,
        PosTerminalClaims? terminal = null,
        bool terminalSelectionRequired = false);

    (string token, DateTimeOffset expiresAtUtc) GenerateRefreshToken();
    string Hash(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
