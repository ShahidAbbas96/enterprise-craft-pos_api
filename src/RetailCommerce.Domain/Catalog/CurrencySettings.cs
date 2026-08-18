using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Catalog;

/// <summary>Singleton row (always exactly one) controlling how money is displayed across the
/// app. Symbol is prepended to formatted amounts (e.g. "Rs. 1,234.00") — leave it blank for
/// plain numbers with no symbol. Deliberately one global setting, not per-store, matching
/// BarcodeSettings.</summary>
public class CurrencySettings : BaseEntity
{
    public string Symbol { get; set; } = "Rs.";
    public int DecimalPlaces { get; set; } = 2;
}
