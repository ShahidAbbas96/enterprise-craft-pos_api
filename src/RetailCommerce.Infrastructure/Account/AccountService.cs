using Microsoft.AspNetCore.Identity;
using RetailCommerce.Application.Account;
using RetailCommerce.Application.Common;
using RetailCommerce.Infrastructure.Identity;

namespace RetailCommerce.Infrastructure.Account;

public class AccountService(UserManager<ApplicationUser> userManager) : IAccountService
{
    public async Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User", userId);
        var roles = await userManager.GetRolesAsync(user);
        return new ProfileDto(user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList());
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User", userId);
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(e => e.Description).ToArray(),
            };
            throw new ValidationAppException(errors);
        }
    }
}
