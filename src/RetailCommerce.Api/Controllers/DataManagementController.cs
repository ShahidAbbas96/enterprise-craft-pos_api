using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.DataManagement;
using RetailCommerce.Infrastructure.DataManagement;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/data-management")]
[Authorize(Policy = "CatalogManagers")]
public class DataManagementController(IDataImportService importService) : ControllerBase
{
    private const long MaxUploadBytes = 20 * 1024 * 1024;

    [HttpPost("products/import")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<ProductImportResultDto>> ImportProducts(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest("The uploaded file is empty.");
        await using var stream = file.OpenReadStream();
        return Ok(await importService.ImportProductsAsync(stream, ct));
    }

    [HttpPost("inventory/import")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<InventoryImportResultDto>> ImportInventory(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest("The uploaded file is empty.");
        await using var stream = file.OpenReadStream();
        return Ok(await importService.ImportInventoryAsync(stream, ct));
    }

    [HttpGet("products/template")]
    public IActionResult ProductTemplate()
    {
        using var wb = DataImportTemplateBuilder.BuildProductTemplate();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product Import Template.xlsx");
    }

    [HttpGet("inventory/template")]
    public IActionResult InventoryTemplate()
    {
        using var wb = DataImportTemplateBuilder.BuildInventoryTemplate();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventory Import Template.xlsx");
    }
}
