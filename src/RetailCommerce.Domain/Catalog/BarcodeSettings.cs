using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Catalog;

/// <summary>Singleton row (always exactly one) controlling what a printed barcode label shows
/// and its physical print size. There's no concept of multiple business profiles in this system,
/// so this is deliberately one global settings row rather than per-store.</summary>
public class BarcodeSettings : BaseEntity
{
    public string CompanyName { get; set; } = "Retail Commerce";
    public bool IncludeCompanyName { get; set; } = true;
    public bool IncludePrice { get; set; } = true;
    public decimal LabelWidthInches { get; set; } = 2.0m;
    public decimal LabelHeightInches { get; set; } = 1.0m;
}
