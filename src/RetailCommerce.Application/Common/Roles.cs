namespace RetailCommerce.Application.Common;

/// <summary>Starting role set per CLAUDE.md §6 — implied by the original product spec but not
/// yet confirmed with the client. Treat as a working default, not a locked-in requirement.</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HeadOffice = "HeadOffice";
    public const string StoreManager = "StoreManager";
    public const string Cashier = "Cashier";
    public const string InventoryManager = "InventoryManager";
    public const string PurchaseManager = "PurchaseManager";

    public static readonly string[] All =
    [
        SuperAdmin, HeadOffice, StoreManager, Cashier, InventoryManager, PurchaseManager,
    ];

    /// <summary>Roles allowed to manage catalog/reference data (products, taxonomy, suppliers, warehouses).</summary>
    public static readonly string[] CatalogManagers = [SuperAdmin, HeadOffice, InventoryManager];

    /// <summary>Roles allowed to manage customers.</summary>
    public static readonly string[] CustomerManagers = [SuperAdmin, HeadOffice, StoreManager, Cashier];

    /// <summary>Roles allowed to create/edit login accounts (roles, Store assignment).</summary>
    public static readonly string[] UserManagers = [SuperAdmin, HeadOffice];
}
