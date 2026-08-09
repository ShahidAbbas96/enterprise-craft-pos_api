using ClosedXML.Excel;
using RetailCommerce.Application.Reports;

namespace RetailCommerce.Infrastructure.Reports;

/// <summary>Builds .xlsx workbooks that mirror the client's legacy "Shop Detailed Sales Report"
/// and "Stock On Hand" exports column-for-column (same header order/text), populated from real
/// data — columns the source tracked that this system has no equivalent for (Family, Range,
/// Transaction ID, Promotion Discount, Manual Discount Reason, Adj Disc %/Adj Discount, Loyalty
/// Discount, Tax Adjustment, Heels Barcode) are left blank rather than fabricated.</summary>
public static class ReportExcelExporter
{
    private static readonly string[] SalesDetailHeaders =
    [
        "Department", "Category", "Sub-Category", "Family", "Range", "Receipt ID", "Phone", "Store Id", "Store Name",
        "Date", "Transaction ID", "Sales Rep ID", "Sales Rep Name", "Transaction Comments", "Discount Name",
        "Item", "Item Description", "Color", "Size", "Barcode", "Sold Price", "Gross Qty", "Gross Sales",
        "Gross Sales Excluding Tax", "Return Qty", "Return Sales", "Return Sales Excluding Tax",
        "Discount Percentage", "Promotion Discount", "Manual Discount Reason", "Adj Disc %", "Adj Discount",
        "Total Discount Amount", "Total Discount Excluding Tax", "Loyalty Discount", "Tax Amount", "Tax Adjustment",
        "Net Qty", "Net Sale", "Net Sale Excluding Tax",
    ];

    public static XLWorkbook BuildSalesDetail(SalesDetailReportDto report, string businessName, string printedBy)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales Detail");

        var row = 1;
        ws.Cell(row, 1).Value = businessName;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        row++;
        ws.Cell(row, 1).Value = "Shop Detailed Sales Report";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 13;
        row += 2;
        ws.Cell(row, 1).Value = $"Printed On: {DateTimeOffset.Now:g}";
        ws.Cell(row, 6).Value = $"Printed by: {printedBy}";
        row += 2;

        ws.Cell(row, 1).Value = "Date"; ws.Cell(row, 2).Value = $"{report.FromDate:dd/MM/yyyy} - {report.ToDate:dd/MM/yyyy}"; row++;
        ws.Cell(row, 1).Value = "Store"; ws.Cell(row, 2).Value = report.StoreFilter; row++;
        ws.Cell(row, 1).Value = "Department"; ws.Cell(row, 2).Value = report.DepartmentFilter; row++;
        row++;

        var headerRow = row;
        for (var c = 0; c < SalesDetailHeaders.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = SalesDetailHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
        row++;

        // Column indices below (22=GrossQty .. 40=NetSaleExcludingTax) follow SalesDetailHeaders
        // order 1:1 — recompute both together if a column is ever added/removed/reordered.
        var totalRow = row;
        ws.Cell(totalRow, 1).Value = "Grand Total";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        var t = report.GrandTotal;
        ws.Cell(totalRow, 22).Value = t.GrossQty;
        ws.Cell(totalRow, 23).Value = t.GrossSales;
        ws.Cell(totalRow, 24).Value = t.GrossSalesExcludingTax;
        ws.Cell(totalRow, 25).Value = t.ReturnQty;
        ws.Cell(totalRow, 26).Value = t.ReturnSales;
        ws.Cell(totalRow, 27).Value = t.ReturnSalesExcludingTax;
        ws.Cell(totalRow, 33).Value = t.TotalDiscountAmount;
        ws.Cell(totalRow, 34).Value = t.TotalDiscountExcludingTax;
        ws.Cell(totalRow, 36).Value = t.TaxAmount;
        ws.Cell(totalRow, 38).Value = t.NetQty;
        ws.Cell(totalRow, 39).Value = t.NetSale;
        ws.Cell(totalRow, 40).Value = t.NetSaleExcludingTax;
        ws.Row(totalRow).Style.Font.Bold = true;
        row++;

        foreach (var l in report.Lines)
        {
            var c = 1;
            ws.Cell(row, c++).Value = l.Department;
            ws.Cell(row, c++).Value = l.Category;
            ws.Cell(row, c++).Value = l.SubCategory;
            ws.Cell(row, c++).Value = l.Family ?? "";
            ws.Cell(row, c++).Value = l.Range ?? "";
            ws.Cell(row, c++).Value = l.ReceiptId;
            ws.Cell(row, c++).Value = l.Phone ?? "";
            ws.Cell(row, c++).Value = l.StoreId;
            ws.Cell(row, c++).Value = l.StoreName;
            ws.Cell(row, c++).Value = l.Date.ToDateTime(TimeOnly.MinValue);
            ws.Cell(row, c - 1).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, c++).Value = l.TransactionId ?? "";
            ws.Cell(row, c++).Value = l.SalesRepId ?? "";
            ws.Cell(row, c++).Value = l.SalesRepName ?? "";
            ws.Cell(row, c++).Value = l.TransactionComments ?? "";
            ws.Cell(row, c++).Value = l.DiscountName ?? "";
            ws.Cell(row, c++).Value = l.Item;
            ws.Cell(row, c++).Value = l.ItemDescription;
            ws.Cell(row, c++).Value = l.Color ?? "";
            ws.Cell(row, c++).Value = l.Size ?? "";
            ws.Cell(row, c++).Value = l.Barcode ?? "";
            ws.Cell(row, c++).Value = l.SoldPrice;
            ws.Cell(row, c++).Value = l.GrossQty;
            ws.Cell(row, c++).Value = l.GrossSales;
            ws.Cell(row, c++).Value = l.GrossSalesExcludingTax;
            ws.Cell(row, c++).Value = l.ReturnQty;
            ws.Cell(row, c++).Value = l.ReturnSales;
            ws.Cell(row, c++).Value = l.ReturnSalesExcludingTax;
            ws.Cell(row, c++).Value = l.DiscountPercentage;
            ws.Cell(row, c++).Value = l.PromotionDiscount;
            ws.Cell(row, c++).Value = l.ManualDiscountReason ?? "";
            ws.Cell(row, c++).Value = l.AdjDiscPercent;
            ws.Cell(row, c++).Value = l.AdjDiscount;
            ws.Cell(row, c++).Value = l.TotalDiscountAmount;
            ws.Cell(row, c++).Value = l.TotalDiscountExcludingTax;
            ws.Cell(row, c++).Value = l.LoyaltyDiscount;
            ws.Cell(row, c++).Value = l.TaxAmount;
            ws.Cell(row, c++).Value = l.TaxAdjustment;
            ws.Cell(row, c++).Value = l.NetQty;
            ws.Cell(row, c++).Value = l.NetSale;
            ws.Cell(row, c).Value = l.NetSaleExcludingTax;
            row++;
        }

        var lastRow = row - 1;
        ApplyCurrencyFormat(ws, headerRow + 1, lastRow, [21, 23, 24, 26, 27, 33, 34, 35, 36, 37, 39, 40]);
        ApplyCurrencyFormat(ws, totalRow, totalRow, [23, 24, 26, 27, 33, 34, 36, 39, 40]);
        ws.Columns().AdjustToContents(1, 60);
        ws.SheetView.FreezeRows(headerRow);
        return wb;
    }

    private static readonly string[] StockOnHandHeaders =
    [
        "Item ID", "Item Name", "Search Name", "Line Item", "Color", "Size", "Store", "BarCode", "Heels Barcode",
        "Retail Price", "On-Hand Quantity", "Retail Value", "Sales Price", "Sales Value",
    ];

    public static XLWorkbook BuildStockOnHand(StockOnHandReportDto report, string businessName, string printedBy)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Stock On Hand");

        var row = 1;
        ws.Cell(row, 1).Value = "Stock On Hand";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        row++;
        ws.Cell(row, 1).Value = businessName;
        row += 2;
        ws.Cell(row, 1).Value = $"Printed On: {DateTimeOffset.Now:g}";
        ws.Cell(row, 6).Value = $"Printed by: {printedBy}";
        row += 2;

        ws.Cell(row, 1).Value = "Store"; ws.Cell(row, 2).Value = report.StoreFilter; row++;
        ws.Cell(row, 1).Value = "Department"; ws.Cell(row, 2).Value = report.DepartmentFilter; row++;
        row++;

        var headerRow = row;
        for (var c = 0; c < StockOnHandHeaders.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = StockOnHandHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
        row++;

        foreach (var l in report.Lines)
        {
            var c = 1;
            ws.Cell(row, c++).Value = l.ItemId;
            ws.Cell(row, c++).Value = l.ItemName;
            ws.Cell(row, c++).Value = l.SearchName;
            ws.Cell(row, c++).Value = l.LineItem;
            ws.Cell(row, c++).Value = l.Color ?? "";
            ws.Cell(row, c++).Value = l.Size ?? "";
            ws.Cell(row, c++).Value = l.Store;
            ws.Cell(row, c++).Value = l.BarCode ?? "";
            ws.Cell(row, c++).Value = l.HeelsBarcode ?? "";
            ws.Cell(row, c++).Value = l.RetailPrice;
            ws.Cell(row, c++).Value = l.OnHandQuantity;
            ws.Cell(row, c++).Value = l.RetailValue;
            ws.Cell(row, c++).Value = l.SalesPrice;
            ws.Cell(row, c).Value = l.SalesValue;
            row++;
        }

        var lastDataRow = row - 1;
        ApplyCurrencyFormat(ws, headerRow + 1, lastDataRow, [10, 12, 13, 14]);

        row++;
        ws.Cell(row, 1).Value = "Grand Total";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 11).Value = report.GrandTotal.OnHandQuantity;
        ws.Cell(row, 12).Value = report.GrandTotal.RetailValue;
        ws.Cell(row, 14).Value = report.GrandTotal.SalesValue;
        ws.Row(row).Style.Font.Bold = true;
        ApplyCurrencyFormat(ws, row, row, [12, 14]);

        ws.Columns().AdjustToContents(1, 60);
        ws.SheetView.FreezeRows(headerRow);
        return wb;
    }

    private static void ApplyCurrencyFormat(IXLWorksheet ws, int fromRow, int toRow, int[] columns)
    {
        foreach (var col in columns)
        {
            ws.Range(fromRow, col, toRow, col).Style.NumberFormat.Format = "#,##0.00";
        }
    }
}
