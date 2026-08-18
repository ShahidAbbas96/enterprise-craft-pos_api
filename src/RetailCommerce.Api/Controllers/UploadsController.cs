using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RetailCommerce.Api.Controllers;

/// <summary>Generic file upload for images referenced elsewhere by URL (currently just
/// Product.ImageUrl). Decoupled from ProductsController on purpose — the product form uploads the
/// image before the product itself is known to exist yet (a brand-new product being created), so
/// this just returns a URL for the caller to stash on whatever DTO field it belongs to.</summary>
[ApiController]
[Route("api/uploads")]
[Authorize(Policy = "CatalogManagers")]
public class UploadsController(IWebHostEnvironment env) : ControllerBase
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    [HttpPost("product-image")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<UploadResultDto>> ProductImage(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest("The uploaded file is empty.");
        if (file.Length > MaxUploadBytes) return BadRequest("Image must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return BadRequest("Only JPG, PNG, WEBP, and GIF images are allowed.");
        }

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        return Ok(new UploadResultDto($"/uploads/products/{fileName}"));
    }
}

public record UploadResultDto(string Url);
