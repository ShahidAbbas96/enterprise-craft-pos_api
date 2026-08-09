namespace RetailCommerce.Application.Common;

/// <summary>Issues sequential, gap-tolerant document numbers (invoices, purchase orders,
/// transfers) backed by a real Postgres sequence — never generate these client-side
/// (the reference prototype's POS used Math.random() for invoice numbers; don't repeat that).</summary>
public interface IDocumentNumberService
{
    /// <param name="storeSegment">Store (or warehouse, when the warehouse has no store) code
    /// inserted into the number for SalesInvoice/PurchaseOrder, e.g. "SINV-ST01-100025". Ignored
    /// for document types that don't carry a store segment.</param>
    Task<string> NextAsync(DocumentType type, string? storeSegment = null, CancellationToken ct = default);
}

public enum DocumentType
{
    SalesInvoice,
    PurchaseOrder,
    Transfer,
    Return,
    Shift,
}
