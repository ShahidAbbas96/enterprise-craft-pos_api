namespace RetailCommerce.Application.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string? LastName,
    Guid? StoreId,
    IReadOnlyList<string> Roles);

public record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    CurrentUserDto User);
