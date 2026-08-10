using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Parties;
using RetailCommerce.Domain.Purchasing;
using RetailCommerce.Domain.Sales;
using RetailCommerce.Domain.Shifts;
using RetailCommerce.Domain.Taxonomy;
using RetailCommerce.Infrastructure.Identity;

namespace RetailCommerce.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Taxonomy
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Gender> Genders => Set<Gender>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<ProductAttributeType> ProductAttributeTypes => Set<ProductAttributeType>();
    public DbSet<ProductAttributeOption> ProductAttributeOptions => Set<ProductAttributeOption>();

    // Catalog
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<BarcodeSettings> BarcodeSettings => Set<BarcodeSettings>();

    // Parties
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Inventory
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    // Sales
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();

    // Shifts
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    // Purchasing
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferLine> TransferLines => Set<TransferLine>();

    // Identity
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Identity tables: keep the default ASP.NET Core Identity schema/behavior but rename
        // away from the default "AspNetUsers" etc. to fit this project's naming.
        builder.Entity<ApplicationUser>(b => b.ToTable("Users"));
        builder.Entity<ApplicationRole>(b => b.ToTable("Roles"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));
    }

    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }
    }
}
