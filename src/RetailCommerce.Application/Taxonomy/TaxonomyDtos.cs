namespace RetailCommerce.Application.Taxonomy;

public record LookupDto(Guid Id, string Code, string Name);

public record CategoryDto(Guid Id, string Code, string Name, Guid DepartmentId);

public record SubcategoryDto(Guid Id, string Code, string Name, Guid CategoryId);

public record CollectionDto(Guid Id, string Name, string VersionLabel, int Year, string DisplayCode, Guid? DepartmentId);

public record AttributeOptionDto(Guid Id, string Code, string Name);

public record AttributeTypeDto(
    Guid Id,
    string Code,
    string Name,
    int DisplayOrder,
    bool IsRequired,
    Guid? DepartmentId,
    IReadOnlyList<AttributeOptionDto> Options);

/// <summary>Everything the Add/Edit Product form needs in one call: every lookup list plus,
/// for each department, which attribute types apply to it (see CLAUDE.md §5.2 — the form must
/// only show Heel/Sole attributes for Footwear, etc.).</summary>
public record TaxonomySnapshotDto(
    IReadOnlyList<LookupDto> Departments,
    IReadOnlyList<LookupDto> Genders,
    IReadOnlyList<LookupDto> EventTypes,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<SubcategoryDto> Subcategories,
    IReadOnlyList<CollectionDto> Collections,
    IReadOnlyList<AttributeTypeDto> AttributeTypes);
