using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RetailCommerce.Application.Common;

namespace RetailCommerce.Infrastructure.Persistence;

public class DocumentNumberService(AppDbContext db) : IDocumentNumberService
{
    public async Task<string> NextAsync(DocumentType type, string? storeSegment = null, CancellationToken ct = default)
    {
        var (sequence, prefix) = type switch
        {
            DocumentType.SalesInvoice => ("invoice_seq", "SINV"),
            DocumentType.PurchaseOrder => ("purchase_order_seq", "PINV"),
            DocumentType.Transfer => ("transfer_seq", "TR"),
            DocumentType.Return => ("return_seq", "RET"),
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

        return string.IsNullOrWhiteSpace(storeSegment) ? $"{prefix}-{next}" : $"{prefix}-{storeSegment}-{next}";
    }
}
