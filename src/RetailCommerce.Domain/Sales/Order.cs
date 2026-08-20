using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Parties;

namespace RetailCommerce.Domain.Sales;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = default!;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public OrderChannel Channel { get; set; } = OrderChannel.Pos;
    public OrderStatus Status { get; set; } = OrderStatus.Completed;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    /// <summary>Snapshot text for the receipt (e.g. "14 August Sale (15%)" or "Manual
    /// discount") — independent of any Discount campaign row, which is just a POS picklist.</summary>
    public string? DiscountLabel { get; set; }

    public Guid? SalesPersonId { get; set; }
    public Employee? SalesPerson { get; set; }

    /// <summary>Which till rang this sale — audit only. Stock/report scoping always uses
    /// WarehouseId; this is never itself the source of truth for what a terminal can access.</summary>
    public Guid? TerminalId { get; set; }
    public PosTerminal? Terminal { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    /// <summary>Client-generated (crypto.randomUUID()) idempotency key for offline-first POS
    /// sync — a retried submission of the same queued sale (e.g. after a lost response) carries
    /// the same value, letting SalesService detect and safely no-op the duplicate instead of
    /// double-selling. Null for sales that were never queued offline (most online sales today
    /// still go through the outbox too, per the POS rewiring, but the field stays optional so
    /// any future direct/admin sale-creation path isn't forced to supply one).</summary>
    public Guid? ClientTransactionId { get; set; }

    /// <summary>True only when this sale was rung up while the POS was offline and is now
    /// arriving via sync — such sales are allowed to drive stock negative (the sale already
    /// physically happened; rejecting it after the fact isn't a valid business action) rather
    /// than throwing ConflictException, with the discrepancy logged to SyncLog for review. Live
    /// online sales keep the existing strict oversell check unchanged.</summary>
    public bool CapturedOffline { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
