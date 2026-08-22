using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RetailCommerce.Infrastructure.Common;

/// <summary>Shared safety-net check for the idempotency pattern used across every offline-sync-
/// capable create (Sale, Customer, Return, Shift, Expense): a fast-path pre-check by
/// ClientTransactionId happens first, but the actual concurrency-safe guarantee against a
/// duplicate is this — catching the Postgres unique-violation on the entity's own filtered
/// ClientTransactionId index when two concurrent retries both slip past the pre-check.</summary>
public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolationOn(this DbUpdateException ex, string constraintName) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
            && pg.ConstraintName == constraintName;
}
