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

        var current = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.ProductId == productId && b.IsCurrent, ct);
        if (current?.Code == barcode)
        {
            return;
        }

        if (current is not null)
        {
            current.IsCurrent = false;
            current.SupersededAtUtc = DateTimeOffset.UtcNow;
        }

        db.ProductBarcodes.Add(new ProductBarcode { ProductId = productId, Code = barcode, IsCurrent = true });
        product.Barcode = barcode;
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignExplicitBarcodeAsync(Guid productId, string barcode, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("Product", productId);
        var trimmed = barcode.Trim();

        var current = await db.ProductBarcodes.FirstOrDefaultAsync(b => b.ProductId == productId && b.IsCurrent, ct);
        if (current?.Code == trimmed)
        {
            return;
        }

        var ownedByAnotherProduct = await db.ProductBarcodes.AnyAsync(b => b.Code == trimmed && b.ProductId != productId, ct);
        if (ownedByAnotherProduct)
        {
            throw new ConflictException($"Barcode '{trimmed}' is already assigned to another product.");
        }

        if (current is not null)
        {
            current.IsCurrent = false;
            current.SupersededAtUtc = DateTimeOffset.UtcNow;
        }

        db.ProductBarcodes.Add(new ProductBarcode { ProductId = productId, Code = trimmed, IsCurrent = true });
        product.Barcode = trimmed;
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
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new ProductBarcodeDto(b.Id, b.Code, b.IsCurrent, b.CreatedAtUtc, b.SupersededAtUtc))
            .ToListAsync(ct);
    }

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
