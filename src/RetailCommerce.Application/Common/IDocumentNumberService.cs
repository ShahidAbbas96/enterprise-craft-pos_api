namespace RetailCommerce.Application.Common;

/// <summary>Issues sequential, gap-tolerant document numbers (invoices, purchase orders,
/// transfers) backed by a real Postgres sequence — never generate these client-side
/// (the reference prototype's POS used Math.random() for invoice numbers; don't repeat that).
/// Deliberately just a short 2-letter prefix + the raw sequence value (e.g. "SI100025") — an
/// earlier version embedded the store/warehouse code (e.g. "SINV-VERY-LONG-CODE-100025"), which
/// had no length ceiling and caused a Postgres "value too long" error in production the moment a
/// real deployment used a longer code than this project's short dev seed codes.</summary>
public interface IDocumentNumberService
{
    Task<string> NextAsync(DocumentType type, CancellationToken ct = default);
}

public enum DocumentType
{
    SalesInvoice,
    PurchaseOrder,
    Transfer,
    Return,
    Shift,
}
