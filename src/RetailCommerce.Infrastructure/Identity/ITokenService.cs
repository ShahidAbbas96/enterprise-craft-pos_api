using System.Security.Claims;

namespace RetailCommerce.Infrastructure.Identity;

public interface ITokenService
{
    (string token, DateTimeOffset expiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles);
    (string token, DateTimeOffset expiresAtUtc) GenerateRefreshToken();
    string Hash(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
