namespace RetailCommerce.Application.Account;

public interface IAccountService
{
    Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
