using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Catalog;

/// <summary>Immutable barcode history row. A barcode, once assigned, is never edited or reused —
/// if a product's Size/Color attributes change later, the old barcode is superseded (IsCurrent
/// flips to false) rather than deleted, so labels/stock already printed with it keep scanning
/// correctly. Exactly one row per product has IsCurrent = true.</summary>
public class ProductBarcode : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public string Code { get; set; } = default!;
    public bool IsCurrent { get; set; }
    public DateTimeOffset? SupersededAtUtc { get; set; }
}
