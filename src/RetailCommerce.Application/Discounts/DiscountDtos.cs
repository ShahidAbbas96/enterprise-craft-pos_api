namespace RetailCommerce.Application.Discounts;

public record DiscountDto(
    Guid Id,
    string Name,
    string Type,
    decimal Value,
    bool IsActive,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? ProductId,
    string? ProductName);

public record UpsertDiscountRequest(
    string Name,
    string Type,
    decimal Value,
    bool IsActive,
    Guid? DepartmentId,
    Guid? ProductId);
