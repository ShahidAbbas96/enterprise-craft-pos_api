using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Purchasing;

/// <summary>Singleton row (always exactly one) — whether Purchase Order screens show each
/// line's product attributes (Color, Size, etc.) alongside SKU/quantity. Deliberately one global
/// setting, not per-store, matching BarcodeSettings/CurrencySettings.</summary>
public class PurchaseOrderSettings : BaseEntity
{
    public bool ShowProductAttributes { get; set; } = true;
}
