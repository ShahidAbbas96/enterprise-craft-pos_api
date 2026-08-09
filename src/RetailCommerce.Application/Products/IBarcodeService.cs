namespace RetailCommerce.Application.Products;

/// <summary>Computes a product's barcode deterministically from its Sku and current Size/Color
/// attribute values ({Sku}-{SizeSegment}-{ColorSegment}, e.g. "K24624002-06-BK"; a missing Size
/// or Color segment defaults to "222"). A barcode is never freely edited: once assigned it is
/// immutable — if the computed value changes (the product's Size/Color attribute values change),
/// the old barcode is superseded and kept in history rather than overwritten, so anything already
/// printed/scanned with it keeps resolving to this product.</summary>
public interface IBarcodeService
{
    /// <summary>Recomputes the product's barcode from its current Sku/Size/Color and, if it
    /// differs from the current one (or none exists yet), supersedes the old one and assigns a
    /// new current barcode. No-op if the computed value is unchanged. Call after a product's
    /// attribute values have been saved.</summary>
    Task EnsureCurrentBarcodeAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Escape hatch for bulk import: preserves a legacy barcode already printed on
    /// physical stock/labels instead of the auto-computed one. Supersedes the current barcode
    /// (kept in history) exactly like a normal reassignment; throws ConflictException if another
    /// product already owns this code. No-op if it's already this product's current barcode.</summary>
    Task AssignExplicitBarcodeAsync(Guid productId, string barcode, CancellationToken ct = default);

    Task<IReadOnlyList<ProductBarcodeDto>> GetHistoryAsync(Guid productId, CancellationToken ct = default);
}
