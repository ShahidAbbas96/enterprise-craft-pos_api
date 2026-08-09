namespace RetailCommerce.Application.Common;

/// <summary>Abstraction over "who is calling this request", implemented in the Api layer from
/// the authenticated ClaimsPrincipal so Application services can be authorization-aware
/// (e.g. store-scoped queries) without depending on ASP.NET Core types directly.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? StoreId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
