using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RetailCommerce.Infrastructure.Identity;

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string token, DateTimeOffset expiresAtUtc) GenerateAccessToken(
        ApplicationUser user,
        IList<string> roles,
        PosTerminalClaims? terminal = null,
        bool terminalSelectionRequired = false)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("first_name", user.FirstName),
        };
        if (user.LastName is { Length: > 0 })
        {
            claims.Add(new Claim("last_name", user.LastName));
        }
        if (terminal is not null)
        {
            claims.Add(new Claim("store_id", terminal.StoreId.ToString()));
            claims.Add(new Claim("terminal_id", terminal.TerminalId.ToString()));
            claims.Add(new Claim("warehouse_id", terminal.WarehouseId.ToString()));
        }
        else if (terminalSelectionRequired)
        {
            // No POS-scoped or back-office access until they pick one — see the startup
            // middleware in Program.cs that gates every route but /api/auth/* on this claim.
            claims.Add(new Claim("terminal_required", "true"));
        }
        else if (user.StoreId is { } storeId)
        {
            claims.Add(new Claim("store_id", storeId.ToString()));
        }
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string token, DateTimeOffset expiresAtUtc) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        return (token, DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            ValidateLifetime = false, // we WANT to read an expired access token during refresh
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, parameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
