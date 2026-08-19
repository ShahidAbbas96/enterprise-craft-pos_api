using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Reports;

/// <summary>Mirrors a "Shop Detailed Sales Report" export from legacy retail POS software,
/// column-for-column, so the Excel export can match that layout exactly. Every field is real
/// data from Orders/OrderLines/Returns except the ones the source report tracks that this
/// system has no equivalent concept for (Family, Range, a Transaction ID distinct from the
/// invoice number, Promotion Discount split out from total discount, Manual Discount Reason,
/// Adj Disc %, Adj Discount, Loyalty Discount amount, Tax Adjustment) — those are always
/// null/zero here rather than fabricated, per explicit instruction.</summary>
public record SalesDetailLineDto(
    string Department,
    string Category,
    string SubCategory,
    string? Family,
    string? Range,
    string ReceiptId,
    string? Phone,
    string StoreId,
    string StoreName,
    DateOnly Date,
    string? TransactionId,
    string? SalesRepId,
    string? SalesRepName,
    string? TransactionComments,
    string? DiscountName,
    string Item,
    string ItemDescription,
    string? Color,
    string? Size,
    string? Barcode,
    decimal SoldPrice,
    int GrossQty,
    decimal GrossSales,
    decimal GrossSalesExcludingTax,
    int ReturnQty,
    decimal ReturnSales,
    decimal ReturnSalesExcludingTax,
    decimal DiscountPercentage,
    decimal PromotionDiscount,
    string? ManualDiscountReason,
    decimal AdjDiscPercent,
    decimal AdjDiscount,
    decimal TotalDiscountAmount,
    decimal TotalDiscountExcludingTax,
    decimal LoyaltyDiscount,
    decimal TaxAmount,
    decimal TaxAdjustment,
    int NetQty,
    decimal NetSale,
    decimal NetSaleExcludingTax);

public record SalesDetailTotalsDto(
    int GrossQty,
    decimal GrossSales,
    decimal GrossSalesExcludingTax,
    int ReturnQty,
    decimal ReturnSales,
    decimal ReturnSalesExcludingTax,
    decimal TotalDiscountAmount,
    decimal TotalDiscountExcludingTax,
    decimal TaxAmount,
    int NetQty,
    decimal NetSale,
    decimal NetSaleExcludingTax);

public record SalesDetailReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    string StoreFilter,
    string DepartmentFilter,
    IReadOnlyList<SalesDetailLineDto> Lines,
    SalesDetailTotalsDto GrandTotal);

public class SalesDetailReportQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? TerminalId { get; set; }
    public Guid? DepartmentId { get; set; }
}
