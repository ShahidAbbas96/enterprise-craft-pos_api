using Microsoft.AspNetCore.Identity;

namespace RetailCommerce.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string? LastName { get; set; }

    /// <summary>Store this user is scoped to (Cashier/Store Manager roles). Null for
    /// head-office/super-admin users who see every store.</summary>
    public Guid? StoreId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
