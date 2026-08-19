namespace RetailCommerce.Application.Common;

/// <summary>Abstraction over "who is calling this request", implemented in the Api layer from
/// the authenticated ClaimsPrincipal so Application services can be authorization-aware
/// (e.g. store-scoped queries) without depending on ASP.NET Core types directly.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? StoreId { get; }

    /// <summary>Set only on a terminal-scoped POS token (minted after login/select-terminal).
    /// Null for back-office tokens, which remain unrestricted across warehouses/stores.</summary>
    Guid? TerminalId { get; }

    /// <summary>The single warehouse a terminal-scoped token is locked to. Null for back-office
    /// tokens.</summary>
    Guid? WarehouseId { get; }

    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);

    /// <summary>The single place every POS-runtime service resolves "which warehouse does this
    /// request actually get to touch". Back-office tokens (no TerminalId claim) pass the
    /// requested value through unchanged — nothing changes for admin/multi-warehouse flows,
    /// including a null/omitted filter meaning "no restriction, show every warehouse". A
    /// terminal-scoped token with the field omitted defaults to its own WarehouseId; if the
    /// caller explicitly names a *different* warehouse, that's treated as tampering and rejected
    /// outright (ForbiddenException) rather than silently rewritten, so a mismatched request
    /// fails loudly instead of quietly succeeding against different data than the client saw.
    /// Use the non-nullable overload for a required field (e.g. CreateSaleRequest.WarehouseId);
    /// use the nullable overload for an optional list/report filter.</summary>
    Guid ResolveWarehouseScope(Guid requested);

    Guid? ResolveWarehouseScope(Guid? requested);

    /// <summary>Same principle as ResolveWarehouseScope, applied to Store — used by Reports and
    /// Dashboard. A terminal-scoped token OR a non-elevated back-office role with a pinned
    /// StoreId (StoreManager) is locked to that store; SuperAdmin/HeadOffice remain unrestricted
    /// across every store, matching the central-vs-POS reporting split.</summary>
    Guid? ResolveStoreScope(Guid? requested);

    /// <summary>Same principle again, applied to Terminal (Sales Detail report's Terminal
    /// filter) — only a terminal-scoped token is pinned; a StoreManager viewing central reports
    /// stays free to pick any terminal within their own (already store-pinned) scope.</summary>
    Guid? ResolveTerminalScope(Guid? requested);
}
