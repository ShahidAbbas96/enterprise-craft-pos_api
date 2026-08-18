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

    [HttpPost("discounts/import")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<DiscountImportResultDto>> ImportDiscounts(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest("The uploaded file is empty.");
        await using var stream = file.OpenReadStream();
        return Ok(await importService.ImportDiscountsAsync(stream, ct));
    }

    [HttpGet("discounts/template")]
    public IActionResult DiscountTemplate()
    {
        const string csv = "itemId,ColorID,SizeID,iDiscountMethod,dDiscountPercentage,dcashDiscount,dDiscountPrice\r\n"
            + "L000162,,,Discount percentage,40,0,0\r\n"
            + "L000163,,,Cash discount amount,0,200,0\r\n"
            + "L000164,,,Discount price,0,0,1700\r\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "Discount Import Template.csv");
    }

    [HttpGet("products/template")]
    public async Task<IActionResult> ProductTemplate(CancellationToken ct)
    {
        var bytes = await importService.BuildProductTemplateAsync(ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product Import Template.xlsx");
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
