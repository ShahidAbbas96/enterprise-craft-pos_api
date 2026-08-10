using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Catalog;

/// <summary>A barcode assigned to a product. A product can carry several — one auto-computed from
/// Sku/Size/Color plus any number of manually-typed alternates (legacy codes, supplier codes,
/// multi-pack codes) — and any of its active ones scan to this product. Exactly one active row
/// per product has IsPrimary = true (the one shown by default on reports/labels/search). A
/// barcode, once assigned, is never edited in place — retiring one flips IsActive off rather
/// than deleting the row, so anything already printed/scanned with it stays traceable.</summary>
public class ProductBarcode : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public string Code { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}
