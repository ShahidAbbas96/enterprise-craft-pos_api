using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Products;
using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Taxonomy;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Products;

public class BarcodeService(AppDbContext db) : IBarcodeService
{
    private const string MissingSegmentDefault = "222";

    public async Task EnsureCurrentBarcodeAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("Product", productId);

        var sizeOption = await db.ProductAttributeValues
            .Where(v => v.ProductId == productId && v.ProductAttributeType.Code == "SIZE")
            .Select(v => v.ProductAttributeOption)
            .FirstOrDefaultAsync(ct);
        var colorOption = await db.ProductAttributeValues
            .Where(v => v.ProductId == productId && v.ProductAttributeType.Code == "COLOR")
            .Select(v => v.ProductAttributeOption)
            .FirstOrDefaultAsync(ct);

        var barcode = $"{product.Sku}-{ResolveSegment(sizeOption)}-{ResolveSegment(colorOption)}";
        await EnsurePrimaryAsync(product, barcode, ct);
    }

    public async Task AssignExplicitBarcodeAsync(Guid productId, string barcode, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("Product", productId);
        await EnsurePrimaryAsync(product, barcode.Trim(), ct);
    }

    /// <summary>Shared by both the auto-computed and explicit-import paths: makes `code` the
    /// product's primary barcode, adding it as a new active row if it isn't already one of this
    /// product's barcodes. The previous primary (if any) stays active, just loses primary status.</summary>
    private async Task EnsurePrimaryAsync(Domain.Catalog.Product product, string code, CancellationToken ct)
    {
        var current = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.ProductId == product.Id && b.IsPrimary, ct);
        if (current?.Code == code)
        {
            return;
        }

        var existing = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.ProductId == product.Id && b.Code == code, ct);
        if (existing is null)
        {
            var ownedByAnotherProduct = await db.ProductBarcodes.AnyAsync(b => b.Code == code && b.ProductId != product.Id, ct);
            if (ownedByAnotherProduct)
            {
                throw new ConflictException($"Barcode '{code}' is already assigned to another product.");
            }
        }

        if (current is not null)
        {
            current.IsPrimary = false;
        }

        if (existing is not null)
        {
            existing.IsPrimary = true;
            existing.IsActive = true;
        }
        else
        {
            db.ProductBarcodes.Add(new ProductBarcode { ProductId = product.Id, Code = code, IsPrimary = true, IsActive = true });
        }

        product.Barcode = code;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProductBarcodeDto> AddManualBarcodeAsync(Guid productId, string barcode, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
        {
            throw new NotFoundException("Product", productId);
        }

        var trimmed = barcode.Trim();
        var taken = await db.ProductBarcodes.AnyAsync(b => b.Code == trimmed, ct);
        if (taken)
        {
            throw new ConflictException($"Barcode '{trimmed}' is already assigned to a product.");
        }

        var entity = new ProductBarcode { ProductId = productId, Code = trimmed, IsPrimary = false, IsActive = true };
        db.ProductBarcodes.Add(entity);
        await db.SaveChangesAsync(ct);
        return new ProductBarcodeDto(entity.Id, entity.Code, entity.IsPrimary, entity.IsActive, entity.CreatedAtUtc);
    }

    public async Task SetPrimaryBarcodeAsync(Guid productId, Guid barcodeId, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("Product", productId);
        var target = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.Id == barcodeId && b.ProductId == productId, ct)
                     ?? throw new NotFoundException("ProductBarcode", barcodeId);

        if (!target.IsActive)
        {
            throw new ConflictException("Cannot make a retired barcode primary — activate it first.");
        }
        if (target.IsPrimary)
        {
            return;
        }

        var current = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.ProductId == productId && b.IsPrimary, ct);
        if (current is not null)
        {
            current.IsPrimary = false;
        }

        target.IsPrimary = true;
        product.Barcode = target.Code;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetBarcodeActiveAsync(Guid productId, Guid barcodeId, bool isActive, CancellationToken ct = default)
    {
        var target = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.Id == barcodeId && b.ProductId == productId, ct)
                     ?? throw new NotFoundException("ProductBarcode", barcodeId);

        if (!isActive && target.IsPrimary)
        {
            throw new ConflictException("Cannot retire the primary barcode — set another barcode as primary first.");
        }

        target.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProductBarcodeDto>> GetHistoryAsync(Guid productId, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
        {
            throw new NotFoundException("Product", productId);
        }

        return await db.ProductBarcodes
            .Where(b => b.ProductId == productId)
            .OrderByDescending(b => b.IsPrimary).ThenByDescending(b => b.CreatedAtUtc)
            .Select(b => new ProductBarcodeDto(b.Id, b.Code, b.IsPrimary, b.IsActive, b.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<BarcodeSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await db.BarcodeSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            // Defensive fallback — DbSeeder creates the singleton row on startup, but never 404
            // just because seeding hasn't run yet.
            var defaults = new BarcodeSettings();
            return ToDto(defaults);
        }
        return ToDto(settings);
    }

    public async Task<BarcodeSettingsDto> UpdateSettingsAsync(UpdateBarcodeSettingsRequest request, CancellationToken ct = default)
    {
        var settings = await db.BarcodeSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new BarcodeSettings();
            db.BarcodeSettings.Add(settings);
        }

        settings.CompanyName = request.CompanyName.Trim();
        settings.IncludeCompanyName = request.IncludeCompanyName;
        settings.IncludePrice = request.IncludePrice;
        settings.LabelWidthInches = request.LabelWidthInches;
        settings.LabelHeightInches = request.LabelHeightInches;
        await db.SaveChangesAsync(ct);
        return ToDto(settings);
    }

    private static BarcodeSettingsDto ToDto(BarcodeSettings s) =>
        new(s.CompanyName, s.IncludeCompanyName, s.IncludePrice, s.LabelWidthInches, s.LabelHeightInches);

    /// <summary>A missing attribute value becomes the fixed placeholder "222". Otherwise: an
    /// admin-set BarcodeCode override always wins; a numeric option Code (sizes like "006")
    /// becomes its value mod 100, zero-padded to 2 digits ("06"); anything else is auto-derived
    /// from the option Name's first 2 letters ("BLACK" -> "BL").</summary>
    private static string ResolveSegment(ProductAttributeOption? option)
    {
        if (option is null)
        {
            return MissingSegmentDefault;
        }

        if (!string.IsNullOrWhiteSpace(option.BarcodeCode))
        {
            return option.BarcodeCode.Trim().ToUpperInvariant();
        }

        if (int.TryParse(option.Code, out var numeric))
        {
            return (numeric % 100).ToString("D2");
        }

        return DeriveShortCode(option.Name);
    }

    private static string DeriveShortCode(string name)
    {
        var letters = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (letters.Length >= 2) return letters[..2];
        if (letters.Length == 1) return letters + "X";
        return "XX";
    }
}
