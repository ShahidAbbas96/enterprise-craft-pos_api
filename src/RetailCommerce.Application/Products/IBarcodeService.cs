namespace RetailCommerce.Application.Products;

/// <summary>Manages a product's barcode(s) and the global print-label settings. A product's
/// primary barcode is computed deterministically from its Sku and current Size/Color attribute
/// values ({Sku}-{SizeSegment}-{ColorSegment}, e.g. "K24624002-06-BK"; a missing Size or Color
/// segment defaults to "222") — but a product can also carry any number of additional active
/// barcodes (manually typed, or preserved from a bulk import) alongside it, all of which resolve
/// to this product. A barcode, once assigned, is never edited in place: retiring one flips it
/// inactive rather than deleting the row, so anything already printed/scanned with it stays
/// traceable.</summary>
public interface IBarcodeService
{
    /// <summary>Recomputes the product's auto barcode from its current Sku/Size/Color. If that
    /// value doesn't already exist as one of the product's barcodes, adds it and makes it
    /// primary (the old primary, if any, stays active — just no longer primary). No-op if the
    /// computed value already matches the current primary. Call after a product's attribute
    /// values have been saved.</summary>
    Task EnsureCurrentBarcodeAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Escape hatch for bulk import: ensures the given legacy code is one of this
    /// product's active barcodes and is its primary one, instead of the auto-computed value.
    /// Throws ConflictException if another product already owns this code. No-op if it's already
    /// this product's primary barcode.</summary>
    Task AssignExplicitBarcodeAsync(Guid productId, string barcode, CancellationToken ct = default);

    /// <summary>Manually adds another active (non-primary) barcode to a product — e.g. a
    /// supplier's own code or a second physical label. Throws ConflictException if the code is
    /// already assigned to any product (including this one).</summary>
    Task<ProductBarcodeDto> AddManualBarcodeAsync(Guid productId, string barcode, CancellationToken ct = default);

    /// <summary>Makes an existing active barcode of this product the primary one (used by
    /// default on reports/labels/search). Throws ConflictException if the target barcode is
    /// inactive.</summary>
    Task SetPrimaryBarcodeAsync(Guid productId, Guid barcodeId, CancellationToken ct = default);

    /// <summary>Activates or retires one of a product's barcodes. Throws ConflictException when
    /// deactivating the current primary barcode — set another one as primary first.</summary>
    Task SetBarcodeActiveAsync(Guid productId, Guid barcodeId, bool isActive, CancellationToken ct = default);

    Task<IReadOnlyList<ProductBarcodeDto>> GetHistoryAsync(Guid productId, CancellationToken ct = default);

    Task<BarcodeSettingsDto> GetSettingsAsync(CancellationToken ct = default);

    Task<BarcodeSettingsDto> UpdateSettingsAsync(UpdateBarcodeSettingsRequest request, CancellationToken ct = default);
}
