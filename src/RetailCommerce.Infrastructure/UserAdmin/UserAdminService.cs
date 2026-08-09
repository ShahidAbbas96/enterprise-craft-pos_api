using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.UserAdmin;
using RetailCommerce.Infrastructure.Identity;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.UserAdmin;

public class UserAdminService(UserManager<ApplicationUser> userManager, AppDbContext db) : IUserAdminService
{
    public async Task<IReadOnlyList<UserListItemDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await userManager.Users.OrderBy(u => u.Email).ToListAsync(ct);
        var storeNames = await db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var result = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserListItemDto(
                user.Id, user.Email!, user.FirstName, user.LastName,
                user.StoreId, user.StoreId is { } sid ? storeNames.GetValueOrDefault(sid) : null,
                user.IsActive, roles.ToList(), user.CreatedAtUtc));
        }
        return result;
    }

    public async Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (request.StoreId is { } storeId && !await db.Stores.AnyAsync(s => s.Id == storeId, ct))
        {
            throw new NotFoundException("Store", storeId);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            StoreId = request.StoreId,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRolesAsync(user, request.Roles);
        return await GetDtoAsync(user.Id, ct);
    }

    public async Task<UserListItemDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("User", id);
        if (request.StoreId is { } storeId && !await db.Stores.AnyAsync(s => s.Id == storeId, ct))
        {
            throw new NotFoundException("Store", storeId);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        user.StoreId = request.StoreId;
        user.IsActive = request.IsActive;
        await userManager.UpdateAsync(user);

        var currentRoles = await userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(request.Roles).ToList();
        var toAdd = request.Roles.Except(currentRoles).ToList();
        if (toRemove.Count > 0) await userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0) await userManager.AddToRolesAsync(user, toAdd);

        return await GetDtoAsync(user.Id, ct);
    }

    private async Task<UserListItemDto> GetDtoAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("User", id);
        var roles = await userManager.GetRolesAsync(user);
        string? storeName = user.StoreId is { } sid
            ? await db.Stores.Where(s => s.Id == sid).Select(s => s.Name).FirstOrDefaultAsync(ct)
            : null;
        return new UserListItemDto(user.Id, user.Email!, user.FirstName, user.LastName, user.StoreId, storeName, user.IsActive, roles.ToList(), user.CreatedAtUtc);
    }
}
