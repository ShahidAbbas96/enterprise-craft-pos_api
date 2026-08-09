namespace RetailCommerce.Application.UserAdmin;

public record UserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string? LastName,
    Guid? StoreId,
    string? StoreName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string? LastName,
    Guid? StoreId,
    IReadOnlyList<string> Roles);

public record UpdateUserRequest(
    string FirstName,
    string? LastName,
    Guid? StoreId,
    bool IsActive,
    IReadOnlyList<string> Roles);
