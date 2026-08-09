namespace RetailCommerce.Application.AttributeAdmin;

public record AttributeTypeItemDto(
    Guid Id,
    string Code,
    string Name,
    int DisplayOrder,
    bool IsRequired,
    Guid? DepartmentId,
    string? DepartmentName,
    int OptionCount);

public record UpsertAttributeTypeRequest(string Code, string Name, int DisplayOrder, bool IsRequired, Guid? DepartmentId);

public record AttributeOptionItemDto(Guid Id, Guid ProductAttributeTypeId, string Code, string Name, bool IsActive, string? BarcodeCode);

public record UpsertAttributeOptionRequest(string Code, string Name, bool IsActive, string? BarcodeCode);
