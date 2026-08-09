namespace RetailCommerce.Application.TaxonomyAdmin;

public record TaxonomyItemDto(Guid Id, string Code, string Name);
public record UpsertTaxonomyItemRequest(string Code, string Name);

public record CategoryItemDto(Guid Id, string Code, string Name, Guid DepartmentId, string DepartmentName);
public record UpsertCategoryRequest(string Code, string Name, Guid DepartmentId);

public record SubcategoryItemDto(Guid Id, string Code, string Name, Guid CategoryId, string CategoryName);
public record UpsertSubcategoryRequest(string Code, string Name, Guid CategoryId);

public record CollectionItemDto(Guid Id, string Name, string VersionLabel, int Year, string DisplayCode, Guid? DepartmentId);
public record UpsertCollectionRequest(string Name, string VersionLabel, int Year, Guid? DepartmentId);
