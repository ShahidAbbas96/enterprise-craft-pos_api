namespace RetailCommerce.Application.PosTerminals;

public record PosTerminalUserDto(Guid UserId, string Email, string FullName);

public record PosTerminalDto(
    Guid Id,
    string Code,
    string Name,
    Guid WarehouseId,
    string WarehouseName,
    Guid? StoreId,
    string? StoreName,
    bool IsActive,
    IReadOnlyList<PosTerminalUserDto> AssignedUsers);

public record UpsertPosTerminalRequest(
    string Code,
    string Name,
    Guid WarehouseId,
    bool IsActive,
    IReadOnlyList<Guid> AssignedUserIds);
