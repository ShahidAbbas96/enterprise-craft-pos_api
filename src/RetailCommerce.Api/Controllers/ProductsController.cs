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

    /// <summary>A product's barcode is computed automatically from its Sku/Size/Color on save —
    /// this returns every barcode it has ever had (current first), since old ones stay valid
    /// forever once printed rather than being overwritten.</summary>
    [HttpGet("{id:guid}/barcode-history")]
    public async Task<ActionResult<IReadOnlyList<ProductBarcodeDto>>> BarcodeHistory(Guid id, CancellationToken ct)
    {
        return Ok(await barcodeService.GetHistoryAsync(id, ct));
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
