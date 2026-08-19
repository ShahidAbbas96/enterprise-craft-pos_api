using System.Security.Claims;
using RetailCommerce.Application.Common;

namespace RetailCommerce.Api.Common;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? StoreId => ParseClaim("store_id");
    public Guid? TerminalId => ParseClaim("terminal_id");
    public Guid? WarehouseId => ParseClaim("warehouse_id");

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public Guid ResolveWarehouseScope(Guid requested) => ResolveWarehouseScope((Guid?)requested)!.Value;

    public Guid? ResolveWarehouseScope(Guid? requested)
    {
        var terminalWarehouseId = WarehouseId;
        if (terminalWarehouseId is null)
        {
            // Back-office token — unrestricted, existing behavior (including null = "no filter").
            return requested;
        }

        if (requested is null || requested == terminalWarehouseId)
        {
            return terminalWarehouseId.Value;
        }

        throw new ForbiddenException("This POS terminal cannot access that warehouse.");
    }

    public Guid? ResolveStoreScope(Guid? requested)
    {
        var pinnedStoreId = StoreId;
        var elevated = IsInRole(RetailCommerce.Application.Common.Roles.SuperAdmin) || IsInRole(RetailCommerce.Application.Common.Roles.HeadOffice);
        if (pinnedStoreId is null || elevated)
        {
            return requested;
        }
        if (requested is null || requested == pinnedStoreId)
        {
            return pinnedStoreId;
        }
        throw new ForbiddenException("You can only view your own store's data.");
    }

    public Guid? ResolveTerminalScope(Guid? requested)
    {
        var pinnedTerminalId = TerminalId;
        if (pinnedTerminalId is null)
        {
            return requested;
        }
        if (requested is null || requested == pinnedTerminalId)
        {
            return pinnedTerminalId;
        }
        throw new ForbiddenException("This POS terminal cannot access another terminal's data.");
    }

    private Guid? ParseClaim(string type)
    {
        var value = User?.FindFirstValue(type);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
