using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Products;

public record ProductAttributeValueDto(
    Guid AttributeTypeId,
    string AttributeTypeCode,
    string AttributeTypeName,
    Guid OptionId,
    string OptionCode,
    string OptionName);

public record ProductDto(
    Guid Id,
    string? ItemCode,
    string Sku,
    string? Barcode,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? GenderId,
    string? GenderName,
    Guid? EventTypeId,
    string? EventTypeName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? SubcategoryId,
    string? SubcategoryName,
    Guid? CollectionId,
    string? CollectionDisplayCode,
    int? Year,
    Guid? SupplierId,
    string? SupplierName,
    decimal Cost,
    decimal Price,
    decimal? WholesalePrice,
    decimal TaxRatePercent,
    decimal DiscountPercent,
    string Unit,
    int MinStock,
    int? MaxStock,
    int ReorderLevel,
    string? Location,
    string Status,
    int TotalStock,
    IReadOnlyList<ProductAttributeValueDto> Attributes,
    DateTimeOffset CreatedAtUtc);

public record ProductAttributeValueInput(Guid ProductAttributeTypeId, Guid ProductAttributeOptionId);

/// <summary>One barcode assigned to a product — a product can have several active at once (an
/// auto-computed Sku-Size-Color one plus any manually-added alternates). IsPrimary marks the one
/// used by default on reports/labels/search; IsActive false means it's been retired (kept for
/// record, no longer offered as current, though the row itself is never deleted).</summary>
public record ProductBarcodeDto(Guid Id, string Code, bool IsPrimary, bool IsActive, DateTimeOffset CreatedAtUtc);

public record AddProductBarcodeRequest(string Code);

/// <summary>Sku/ItemCode are optional here on purpose: the Product form never sends them (the
/// server always generates Sku, and generates ItemCode when not given) while bulk Excel import
/// can still supply explicit values it wants trusted verbatim — see ProductService.CreateAsync/
/// UpdateAsync for the exact "null = let the system decide" rule.</summary>
public record UpsertProductRequest(
    string? ItemCode,
    string? Sku,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? DepartmentId,
    Guid? GenderId,
    Guid? EventTypeId,
    Guid? CategoryId,
    Guid? SubcategoryId,
    Guid? CollectionId,
    int? Year,
    Guid? SupplierId,
    decimal Cost,
    decimal Price,
    decimal? WholesalePrice,
    decimal TaxRatePercent,
    decimal DiscountPercent,
    string Unit,
    int MinStock,
    int? MaxStock,
    int ReorderLevel,
    string? Location,
    string Status,
    IReadOnlyList<ProductAttributeValueInput> Attributes,
    Guid? InitialStockWarehouseId,
    int? InitialStockQuantity);

public class ProductListQuery : PagedQuery
{
    public Guid? DepartmentId { get; set; }
    public Guid? GenderId { get; set; }
    public Guid? EventTypeId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public Guid? CollectionId { get; set; }
    public int? Year { get; set; }
    public string? Status { get; set; }
}

/// <summary>One row of Settings → Product Fields. FieldKey is a fixed, backend-owned identifier
/// (see ProductFieldSettingsService.KnownFields) — the admin can only change State, never add or
/// remove rows. Sku/ItemCode never appear here (see ProductFieldConfig's doc comment).</summary>
public record ProductFieldConfigDto(string FieldKey, string DisplayName, string State);

public record UpdateProductFieldConfigRequest(string FieldKey, string State);
