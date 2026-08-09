namespace RetailCommerce.Application.UserAdmin;

/// <summary>Manages login accounts (distinct from Employees, the no-login POS sales-person
/// roster). Only route by which a new cashier/store-manager account — and the Store they're
/// scoped to — can be created; before this, only the seeded SuperAdmin could log in at all.</summary>
public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItemDto>> ListAsync(CancellationToken ct = default);
    Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserListItemDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
}
