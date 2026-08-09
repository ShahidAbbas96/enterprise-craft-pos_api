using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.UserAdmin;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/users-admin")]
[Authorize(Policy = "UserManagers")]
public class UsersAdminController(IUserAdminService userAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken ct) =>
        Ok(await userAdminService.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> Create(CreateUserRequest request, CancellationToken ct) =>
        Ok(await userAdminService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserListItemDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await userAdminService.UpdateAsync(id, request, ct));
}
