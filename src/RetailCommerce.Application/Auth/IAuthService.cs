namespace RetailCommerce.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Called after login when the user has 2+ assigned terminals — validates the
    /// chosen terminal is actually one of theirs and active, then mints a fresh, fully-scoped
    /// token pair (replacing the access-restricted one login returned).</summary>
    Task<AuthResponse> SelectTerminalAsync(Guid userId, Guid terminalId, string? ipAddress, CancellationToken ct = default);
}
