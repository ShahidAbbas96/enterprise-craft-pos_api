using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Catalog;

/// <summary>Singleton row (always exactly one) controlling which layout the POS terminal
/// renders — "Default" (tile grid of products, cart on the side) or "List" (SKU/barcode
/// quick-entry into a line-item table, closer to legacy retail POS software). Deliberately one
/// global setting, not per-store/per-terminal, matching BarcodeSettings/CurrencySettings.</summary>
public class PosSettings : BaseEntity
{
    public string ViewMode { get; set; } = "Default";

    /// <summary>How many days back "Search Slip" looks when listing recent receipts for
    /// return/exchange — the store's return policy window (e.g. 15 = returns/exchanges accepted
    /// within 15 days of sale). Purely a UI filter default, not enforced server-side.</summary>
    public int ReturnPolicyDays { get; set; } = 15;

    /// <summary>Which Product fields (and custom attribute types) show on the POS product-details
    /// card and product tile grid, stored comma-separated. Each entry is either a core field key
    /// understood by the frontend (e.g. "sku", "barcode", "price") or "attr:{attributeTypeId}" for
    /// a custom attribute (Color, Size, ...). The backend treats these as opaque strings — the
    /// Angular POS module owns the field catalog and how each key is resolved/formatted.</summary>
    public string VisibleProductFields { get; set; } = "sku,barcode,price,totalStock";
}
