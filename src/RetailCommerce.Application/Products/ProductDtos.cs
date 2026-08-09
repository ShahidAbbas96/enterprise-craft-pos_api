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
    Guid DepartmentId,
    string DepartmentName,
    Guid GenderId,
    string GenderName,
    Guid EventTypeId,
    string EventTypeName,
    Guid CategoryId,
    string CategoryName,
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

/// <summary>A product's barcode is a computed, immutable identifier (Sku-Size-Color) — never
/// accepted as free-text input. This history row shows one assignment; SupersededAtUtc is set
/// when a later attribute change caused a new current barcode to be assigned.</summary>
public record ProductBarcodeDto(Guid Id, string Code, bool IsCurrent, DateTimeOffset CreatedAtUtc, DateTimeOffset? SupersededAtUtc);

public record UpsertProductRequest(
    string? ItemCode,
    string Sku,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid DepartmentId,
    Guid GenderId,
    Guid EventTypeId,
    Guid CategoryId,
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
