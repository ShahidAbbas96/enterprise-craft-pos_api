using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RetailCommerce.Application.Common;

namespace RetailCommerce.Infrastructure.Persistence;

public class DocumentNumberService(AppDbContext db) : IDocumentNumberService
{
    public async Task<string> NextAsync(DocumentType type, CancellationToken ct = default)
    {
        // 2-letter prefixes, no store segment, no separator — keeps the generated number under
        // 10 characters for the life of the sequence (a 2-char prefix + up to 8 digits is 10
        // digits' worth of headroom, i.e. up to 99,999,999 documents, before this needs revisiting).
        var (sequence, prefix) = type switch
        {
            DocumentType.SalesInvoice => ("invoice_seq", "SI"),
            DocumentType.PurchaseOrder => ("purchase_order_seq", "PO"),
            DocumentType.Transfer => ("transfer_seq", "TR"),
            DocumentType.Return => ("return_seq", "RT"),
            DocumentType.Shift => ("shift_seq", "SH"),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        // Deliberately does not dispose the connection — it's owned and pooled by this
        // DbContext instance (which may already be mid-transaction from the caller).
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        if (db.Database.CurrentTransaction is { } transaction)
        {
            command.Transaction = transaction.GetDbTransaction();
        }
        command.CommandText = $"SELECT nextval('{sequence}')";
        var result = await command.ExecuteScalarAsync(ct);
        var next = Convert.ToInt64(result);

        return $"{prefix}{next}";
    }
}
