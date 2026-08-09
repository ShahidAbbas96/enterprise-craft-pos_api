using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Reports;
using RetailCommerce.Infrastructure.Reports;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IReportsService reportsService) : ControllerBase
{
    private const string BusinessName = "Retail Commerce";

    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> Sales([FromQuery] int days, CancellationToken ct) =>
        Ok(await reportsService.GetSalesReportAsync(days <= 0 ? 30 : days, ct));

    [HttpGet("top-products")]
    public async Task<ActionResult> TopProducts([FromQuery] int days, [FromQuery] int limit, CancellationToken ct) =>
        Ok(await reportsService.GetTopProductsAsync(days <= 0 ? 30 : days, limit <= 0 ? 10 : limit, ct));

    [HttpGet("inventory-valuation")]
    public async Task<ActionResult> InventoryValuation(CancellationToken ct) =>
        Ok(await reportsService.GetInventoryValuationAsync(ct));

    [HttpGet("sales-detail")]
    public async Task<ActionResult<SalesDetailReportDto>> SalesDetail([FromQuery] SalesDetailReportQuery query, CancellationToken ct) =>
        Ok(await reportsService.GetSalesDetailReportAsync(query, ct));

    [HttpGet("sales-detail/export")]
    public async Task<IActionResult> ExportSalesDetail([FromQuery] SalesDetailReportQuery query, CancellationToken ct)
    {
        var report = await reportsService.GetSalesDetailReportAsync(query, ct);
        using var wb = ReportExcelExporter.BuildSalesDetail(report, BusinessName, CurrentUserDisplayName);
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var fileName = $"Shop Detailed Sales Report {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("stock-on-hand")]
    public async Task<ActionResult<StockOnHandReportDto>> StockOnHand([FromQuery] StockOnHandQuery query, CancellationToken ct) =>
        Ok(await reportsService.GetStockOnHandReportAsync(query, ct));

    [HttpGet("stock-on-hand/export")]
    public async Task<IActionResult> ExportStockOnHand([FromQuery] StockOnHandQuery query, CancellationToken ct)
    {
        var report = await reportsService.GetStockOnHandReportAsync(query, ct);
        using var wb = ReportExcelExporter.BuildStockOnHand(report, BusinessName, CurrentUserDisplayName);
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var fileName = $"Stock On Hand Report {DateTimeOffset.Now:yyyy-MM-dd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private string CurrentUserDisplayName
    {
        get
        {
            var first = User.FindFirstValue("first_name");
            var last = User.FindFirstValue("last_name");
            var combined = $"{first}{(string.IsNullOrEmpty(last) ? "" : " " + last)}".Trim();
            return combined.Length > 0 ? combined : (User.FindFirstValue(ClaimTypes.Email) ?? "System");
        }
    }
}
