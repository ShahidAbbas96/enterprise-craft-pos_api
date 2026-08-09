using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Inventory;

/// <summary>A physical selling location. Distinct from Warehouse (which holds stock) because a
/// store can have zero or more backroom/associated warehouses, and central distribution
/// warehouses are not tied to any single store. Relationship confirmed as 1 store : many
/// warehouses as a working assumption — see CLAUDE.md open questions.</summary>
public class Store : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Tax registration numbers, printed on the receipt if present. Purely
    /// informational — this system has no FBR e-invoicing integration, so no invoice
    /// number/QR code is generated or claimed to be verifiable.</summary>
    public string? Ntn { get; set; }
    public string? Strn { get; set; }

    /// <summary>Free-text refund/exchange policy printed at the bottom of the receipt. Falls
    /// back to a generic default when not set — see InvoicePreviewComponent.</summary>
    public string? ReceiptFooterText { get; set; }

    public PartyStatus Status { get; set; } = PartyStatus.Active;

    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
