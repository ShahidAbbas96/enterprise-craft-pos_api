using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailCommerce.Application.Account;
using RetailCommerce.Application.AttributeAdmin;
using RetailCommerce.Application.Auth;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Customers;
using RetailCommerce.Application.Dashboard;
using RetailCommerce.Application.DataManagement;
using RetailCommerce.Application.Discounts;
using RetailCommerce.Application.Employees;
using RetailCommerce.Application.Inventory;
using RetailCommerce.Application.Products;
using RetailCommerce.Application.Purchasing;
using RetailCommerce.Application.Reports;
using RetailCommerce.Application.Returns;
using RetailCommerce.Application.Sales;
using RetailCommerce.Application.Shifts;
using RetailCommerce.Application.Stores;
using RetailCommerce.Application.Suppliers;
using RetailCommerce.Application.Taxonomy;
using RetailCommerce.Application.TaxonomyAdmin;
using RetailCommerce.Application.UserAdmin;
using RetailCommerce.Application.Warehouses;
using RetailCommerce.Infrastructure.Account;
using RetailCommerce.Infrastructure.AttributeAdmin;
using RetailCommerce.Infrastructure.Customers;
using RetailCommerce.Infrastructure.Dashboard;
using RetailCommerce.Infrastructure.DataManagement;
using RetailCommerce.Infrastructure.Discounts;
using RetailCommerce.Infrastructure.Employees;
using RetailCommerce.Infrastructure.Identity;
using RetailCommerce.Infrastructure.Inventory;
using RetailCommerce.Infrastructure.Persistence;
using RetailCommerce.Infrastructure.Products;
using RetailCommerce.Infrastructure.Purchasing;
using RetailCommerce.Infrastructure.Reports;
using RetailCommerce.Infrastructure.Returns;
using RetailCommerce.Infrastructure.Sales;
using RetailCommerce.Infrastructure.Shifts;
using RetailCommerce.Infrastructure.Stores;
using RetailCommerce.Infrastructure.Suppliers;
using RetailCommerce.Infrastructure.Taxonomy;
using RetailCommerce.Infrastructure.TaxonomyAdmin;
using RetailCommerce.Infrastructure.UserAdmin;
using RetailCommerce.Infrastructure.Warehouses;

namespace RetailCommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 8;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITaxonomyService, TaxonomyService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<ITaxonomyAdminService, TaxonomyAdminService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IReturnsService, ReturnsService>();
        services.AddScoped<IAttributeAdminService, AttributeAdminService>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        return services;
    }
}
