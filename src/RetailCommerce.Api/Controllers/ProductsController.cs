using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Products;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductService productService, IBarcodeService barcodeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] ProductListQuery query, CancellationToken ct)
    {
        return Ok(await productService.ListAsync(query, ct));
    }

    /// <summary>A product's primary barcode is computed automatically from its Sku/Size/Color on
    /// save, but it can also carry other active barcodes (manually added, or preserved from
    /// import) — this returns all of them, primary first.</summary>
    [HttpGet("{id:guid}/barcode-history")]
    public async Task<ActionResult<IReadOnlyList<ProductBarcodeDto>>> BarcodeHistory(Guid id, CancellationToken ct)
    {
        return Ok(await barcodeService.GetHistoryAsync(id, ct));
    }

    [HttpPost("{id:guid}/barcodes")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<ProductBarcodeDto>> AddBarcode(Guid id, AddProductBarcodeRequest request, CancellationToken ct)
    {
        return Ok(await barcodeService.AddManualBarcodeAsync(id, request.Code, ct));
    }

    [HttpPut("{id:guid}/barcodes/{barcodeId:guid}/primary")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> SetPrimaryBarcode(Guid id, Guid barcodeId, CancellationToken ct)
    {
        await barcodeService.SetPrimaryBarcodeAsync(id, barcodeId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/barcodes/{barcodeId:guid}/active")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> SetBarcodeActive(Guid id, Guid barcodeId, [FromQuery] bool isActive, CancellationToken ct)
    {
        await barcodeService.SetBarcodeActiveAsync(id, barcodeId, isActive, ct);
        return NoContent();
    }

    [HttpGet("barcode-settings")]
    public async Task<ActionResult<BarcodeSettingsDto>> GetBarcodeSettings(CancellationToken ct)
    {
        return Ok(await barcodeService.GetSettingsAsync(ct));
    }

    [HttpPut("barcode-settings")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<BarcodeSettingsDto>> UpdateBarcodeSettings(UpdateBarcodeSettingsRequest request, CancellationToken ct)
    {
        return Ok(await barcodeService.UpdateSettingsAsync(request, ct));
    }

    [HttpGet("field-config")]
    public async Task<ActionResult<IReadOnlyList<ProductFieldConfigDto>>> GetFieldConfig(CancellationToken ct)
    {
        return Ok(await productService.GetFieldConfigAsync(ct));
    }

    [HttpPut("field-config")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<IReadOnlyList<ProductFieldConfigDto>>> UpdateFieldConfig(IReadOnlyList<UpdateProductFieldConfigRequest> request, CancellationToken ct)
    {
        return Ok(await productService.UpdateFieldConfigAsync(request, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await productService.GetAsync(id, ct));
    }

    [HttpPost]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<ProductDto>> Create(UpsertProductRequest request, CancellationToken ct)
    {
        var created = await productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpsertProductRequest request, CancellationToken ct)
    {
        return Ok(await productService.UpdateAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
