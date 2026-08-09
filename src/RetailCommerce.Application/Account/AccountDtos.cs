namespace RetailCommerce.Application.Account;

public record ProfileDto(Guid Id, string Email, string FirstName, string? LastName, IReadOnlyList<string> Roles);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
