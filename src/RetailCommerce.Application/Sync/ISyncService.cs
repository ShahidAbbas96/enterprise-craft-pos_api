using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Sync;

/// <summary>Feeds an offline-first POS terminal's local database. Both BootstrapAsync and
/// PullAsync are scoped to the caller's own terminal/warehouse via ICurrentUserService — a
/// terminal can never pull another store's data through this service, mirroring the same
/// Resolve*Scope enforcement already used across Sales/Inventory/Products.</summary>
public interface ISyncService
{
    /// <summary>Full snapshot, ignoring any prior state — used on first login/select-terminal, or
    /// after an explicit "Force Resync" from the Sync Status panel.</summary>
    Task<SyncSnapshotDto> BootstrapAsync(CancellationToken ct = default);

    /// <summary>Delta since the terminal's last successful pull. A null cursor behaves like
    /// BootstrapAsync (first-ever pull has nothing to diff against).</summary>
    Task<SyncSnapshotDto> PullAsync(DateTimeOffset? since, CancellationToken ct = default);

    /// <summary>Recent orders for the caller's own resolved warehouse, windowed to roughly
    /// PosSettings.ReturnPolicyDays and delta-filterable by `since` exactly like the main
    /// snapshot's Products/Customers/Discounts — feeds the offline POS's local "recent orders"
    /// cache (Search Slip, POS Reports, Returns lookup). A null cursor returns the full window.</summary>
    Task<IReadOnlyList<OrderSyncDto>> PullOrdersAsync(DateTimeOffset? since, CancellationToken ct = default);

    Task<PagedResult<SyncLogDto>> ListLogsAsync(SyncLogListQuery query, CancellationToken ct = default);
}
