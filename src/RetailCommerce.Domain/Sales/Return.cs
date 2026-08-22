using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Parties;

namespace RetailCommerce.Domain.Sales;

/// <summary>Records what came back against a past sale and restocks inventory — no refund
/// payment-method tracking (just what was returned, and how much it's worth).</summary>
public class Return : BaseEntity
{
    public string ReturnNumber { get; set; } = default!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? Reason { get; set; }
    public decimal Total { get; set; }

    public Guid? CreatedByUserId { get; set; }

    /// <summary>Client-generated idempotency key from the POS offline outbox — same pattern as
    /// Order.ClientTransactionId/Customer.ClientTransactionId. Null for a return created directly
    /// online (e.g. today's live Returns screen, which has no outbox).</summary>
    public Guid? ClientTransactionId { get; set; }

    public ICollection<ReturnLine> Lines { get; set; } = new List<ReturnLine>();
}
