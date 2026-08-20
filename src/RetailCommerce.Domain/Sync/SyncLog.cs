using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Sync;

public enum SyncDirection
{
    Push,
    Pull,
}

public enum SyncLogStatus
{
    Success,
    /// <summary>The idempotency check caught a retry of an already-processed transaction — not
    /// an error, but worth a distinct status so the Sync Log screen can tell "handled duplicate"
    /// apart from "genuinely new" at a glance.</summary>
    Duplicate,
    /// <summary>Processed successfully but with a business-rule exception worth reviewing —
    /// e.g. an offline-captured sale that drove stock negative.</summary>
    Warning,
    Failed,
}

/// <summary>Audit trail for offline-sync push/pull activity — answers "what was synced, when,
/// by which terminal, did it succeed" for the Settings → Sync Log troubleshooting screen. Written
/// at the idempotency-check point in SalesService.CreateSaleAsync and by the pull endpoint.</summary>
public class SyncLog : BaseEntity
{
    public Guid? TerminalId { get; set; }

    public SyncDirection Direction { get; set; }

    /// <summary>e.g. "Order", "Products" (a pull batch isn't tied to one entity) — free text
    /// rather than an enum since pull batches cover several entity types per call.</summary>
    public string EntityType { get; set; } = default!;

    public Guid? EntityId { get; set; }

    public Guid? ClientTransactionId { get; set; }

    public SyncLogStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
