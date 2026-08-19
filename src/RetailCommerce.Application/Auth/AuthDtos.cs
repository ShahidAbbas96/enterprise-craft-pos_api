namespace RetailCommerce.Application.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record SelectTerminalRequest(Guid TerminalId);

/// <summary>One of the user's assigned, active POS terminals — offered to the client so it can
/// render a picker when a user has more than one.</summary>
public record TerminalOptionDto(Guid Id, string Code, string Name, string StoreName);

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string? LastName,
    Guid? StoreId,
    IReadOnlyList<string> Roles,
    Guid? TerminalId,
    Guid? WarehouseId,
    string? TerminalName,
    string? StoreName);

public record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    CurrentUserDto User,
    /// <summary>Populated only when the user has 2+ assigned terminals and none is selected yet
    /// — the returned token is access-restricted (see PosTerminalClaims/terminal_required) until
    /// the client calls POST /api/auth/select-terminal with one of these ids.</summary>
    IReadOnlyList<TerminalOptionDto>? AvailableTerminals = null);
