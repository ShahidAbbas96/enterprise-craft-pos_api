using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetailCommerce.Application.Common;
using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Taxonomy;
using RetailCommerce.Infrastructure.Identity;
using RetailCommerce.Infrastructure.Products;

namespace RetailCommerce.Infrastructure.Persistence.Seed;

/// <summary>
/// <see cref="BootstrapAsync"/> is safe against a real/production database — it only applies
/// migrations and ensures roles + a single default admin login exist, no sample data. Every step
/// is idempotent (guarded so it only inserts once), so it's safe to run on every process startup
/// in every environment.
///
/// <see cref="SeedDemoDataAsync"/> is dev/staging-only sample data and is never called outside
/// <c>IsDevelopment()</c> (see Program.cs) — the admin password it shares is a well-known
/// local-dev-only value that must be rotated before any real deployment.
///
/// The taxonomy, categories, subcategories, attribute types/options and collections seeded by
/// <see cref="SeedDemoDataAsync"/> are transcribed directly from the client's "DESIRE ITEM MASTER
/// HIERARCHY.xlsx" sample (62 rows) — see CLAUDE.md §5. The sample products reuse that sheet's
/// ItemCode/department/taxonomy combinations, but Cost/Price/stock figures are placeholder dev
/// values (the source sheet had no pricing) and must be replaced with real data before go-live.
/// </summary>
public static class DbSeeder
{
    private const string DevAdminPassword = "ChangeMe!123";

    /// <summary>Runs on every startup in every environment: applies pending EF Core migrations
    /// (creating the schema on a brand-new database), ensures the built-in roles and a default
    /// admin login exist, and keeps barcode settings/backfill in sync. Contains no sample/demo
    /// data — safe to point at a real deployment's database.</summary>
    public static async Task BootstrapAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, logger);

        // Existing products' old random-EAN13 barcodes are invalid under the new
        // {Sku}-{Size}-{Color} scheme, and any product a future import adds without going
        // through ProductService also needs one. Idempotent: only touches products with no
        // current barcode row yet — a no-op on a database with no products.
        await BackfillProductBarcodesAsync(db, logger);
        await EnsureBarcodeSettingsAsync(db);
    }

    /// <summary>Dev/staging-only sample taxonomy + catalog + demo store/warehouses (see class
    /// remarks). Only ever called from Program.cs behind <c>IsDevelopment()</c> — never call this
    /// against a database meant to hold a real store's data.</summary>
    public static async Task SeedDemoDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        if (await db.Departments.AnyAsync())
        {
            logger.LogInformation("Seed data already present — skipping taxonomy/catalog seed.");
            return;
        }

        var departments = await SeedDepartmentsAsync(db);
        var genders = await SeedGendersAsync(db);
        var eventTypes = await SeedEventTypesAsync(db);
        var categories = await SeedCategoriesAsync(db, departments);
        var subcategories = await SeedSubcategoriesAsync(db, categories);
        var collections = await SeedCollectionsAsync(db, departments);
        var attributeOptions = await SeedAttributeTypesAsync(db, departments);
        var (stores, warehouses) = await SeedStoresAndWarehousesAsync(db);
        await SeedSampleProductsAsync(db, departments, genders, eventTypes, categories, subcategories, collections, attributeOptions, warehouses);

        logger.LogInformation("Seed data created successfully.");
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string email = "admin@retailcommerce.local";
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
        };

        var result = await userManager.CreateAsync(admin, DevAdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
            logger.LogWarning(
                "Seeded dev admin user {Email} with a well-known local password — rotate before any shared/staging use.",
                email);
        }
        else
        {
            logger.LogError("Failed to seed admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task BackfillProductBarcodesAsync(AppDbContext db, ILogger logger)
    {
        var productIds = await db.Products
            .Where(p => !db.ProductBarcodes.Any(b => b.ProductId == p.Id && b.IsPrimary))
            .Select(p => p.Id)
            .ToListAsync();

        if (productIds.Count == 0) return;

        var barcodeService = new BarcodeService(db);
        foreach (var id in productIds)
        {
            await barcodeService.EnsureCurrentBarcodeAsync(id);
        }

        logger.LogInformation("Backfilled deterministic barcodes for {Count} product(s).", productIds.Count);
    }

    private static async Task EnsureBarcodeSettingsAsync(AppDbContext db)
    {
        if (await db.BarcodeSettings.AnyAsync()) return;
        db.BarcodeSettings.Add(new Domain.Catalog.BarcodeSettings());
        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Department>> SeedDepartmentsAsync(AppDbContext db)
    {
        var departments = new[]
        {
            new Department { Code = "FOOTWEAR", Name = "Footwear" },
            new Department { Code = "BAGS", Name = "Bags" },
            new Department { Code = "JEWELLERY", Name = "Jewellery" },
        };
        db.Departments.AddRange(departments);
        await db.SaveChangesAsync();
        return departments.ToDictionary(x => x.Code);
    }

    private static async Task<Dictionary<string, Gender>> SeedGendersAsync(AppDbContext db)
    {
        // Only WOMEN and KIDS were present in the client's sample — MEN/UNISEX are not seeded
        // to avoid inventing data; add them once the client confirms (CLAUDE.md open question).
        var genders = new[]
        {
            new Gender { Code = "WOMEN", Name = "Women" },
            new Gender { Code = "KIDS", Name = "Kids" },
        };
        db.Genders.AddRange(genders);
        await db.SaveChangesAsync();
        return genders.ToDictionary(x => x.Code);
    }

    private static async Task<Dictionary<string, EventType>> SeedEventTypesAsync(AppDbContext db)
    {
        var eventTypes = new[]
        {
            new EventType { Code = "BASIC", Name = "Basic" },
            new EventType { Code = "CASUAL", Name = "Casual" },
            new EventType { Code = "FORMAL", Name = "Formal" },
            new EventType { Code = "COTURE", Name = "Coture" },
        };
        db.EventTypes.AddRange(eventTypes);
        await db.SaveChangesAsync();
        return eventTypes.ToDictionary(x => x.Code);
    }

    private static async Task<Dictionary<(string Dept, string Code), Category>> SeedCategoriesAsync(
        AppDbContext db, Dictionary<string, Department> departments)
    {
        (string Dept, string Code, string Name)[] rows =
        [
            ("FOOTWEAR", "KHUSSA", "Khussa"),
            ("FOOTWEAR", "SANDAL", "Sandal"),
            ("FOOTWEAR", "SLIPPER", "Slipper"),
            ("BAGS", "HAND CARRY", "Hand Carry"),
            ("BAGS", "SHOULDER", "Shoulder"),
            ("BAGS", "CROSS BODY", "Cross Body"),
            ("JEWELLERY", "EARRINGS", "Earrings"),
            ("JEWELLERY", "NECKLACE", "Necklace"),
            ("JEWELLERY", "RING", "Ring"),
            ("JEWELLERY", "BANGLES", "Bangles"),
            ("JEWELLERY", "HEAD PEACE", "Head Piece"),
        ];

        var categories = rows.Select(r => new Category { DepartmentId = departments[r.Dept].Id, Code = r.Code, Name = r.Name }).ToArray();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var result = new Dictionary<(string, string), Category>();
        for (var i = 0; i < rows.Length; i++)
        {
            result[(rows[i].Dept, rows[i].Code)] = categories[i];
        }
        return result;
    }

    private static async Task<Dictionary<(string Cat, string Code), Subcategory>> SeedSubcategoriesAsync(
        AppDbContext db, Dictionary<(string, string), Category> categories)
    {
        (string Cat, string Code, string Name)[] rows =
        [
            ("KHUSSA", "REGULAR", "Regular"),
            ("KHUSSA", "PESHAWARI KHUSSA", "Peshawari Khussa"),
            ("SANDAL", "SHELL TOE", "Shell Toe"),
            ("SANDAL", "NARROW BAND", "Narrow Band"),
            ("SLIPPER", "BROAD BAND", "Broad Band"),
            ("SLIPPER", "D3-BAND", "D3-Band"),
            ("HAND CARRY", "CLUTCHES", "Clutches"),
            ("HAND CARRY", "POTLI", "Potli"),
            ("SHOULDER", "TOTE BAG", "Tote Bag"),
            ("SHOULDER", "HOBO", "Hobo"),
            ("CROSS BODY", "POTLI", "Potli"),
            ("EARRINGS", "Earings", "Earrings"),
            ("EARRINGS", "Default", "Default"),
            ("NECKLACE", "PENDENT", "Pendant"),
            ("RING", "SLEEK FINGER RING", "Sleek Finger Ring"),
            ("RING", "THICK FINGER RING", "Thick Finger Ring"),
            ("BANGLES", "BANGLES", "Bangles"),
            ("HEAD PEACE", "BANGLES", "Bangles"),
        ];

        // Category codes repeat across departments (e.g. none here do, but keep lookup by code only
        // since Subcategory belongs to one specific Category row already resolved via categories dict).
        var byCode = categories.Values.GroupBy(c => c.Code).ToDictionary(g => g.Key, g => g.First());

        var subcategories = rows.Select(r => new Subcategory { CategoryId = byCode[r.Cat].Id, Code = r.Code, Name = r.Name }).ToArray();
        db.Subcategories.AddRange(subcategories);
        await db.SaveChangesAsync();

        var result = new Dictionary<(string, string), Subcategory>();
        for (var i = 0; i < rows.Length; i++)
        {
            result[(rows[i].Cat, rows[i].Code)] = subcategories[i];
        }
        return result;
    }

    private static async Task<Dictionary<string, Collection>> SeedCollectionsAsync(AppDbContext db, Dictionary<string, Department> departments)
    {
        (string Name, string Version, int Year)[] rows =
        [
            ("Aarinda Potli", "V1", 2024),
            ("Aashvi", "V1", 2024),
            ("Bagh-e-Noor", "V1", 2025),
            ("Dazzle Jewellery", "V1", 2024),
            ("Dazzle Saughat", "V2", 2026),
            ("Dazzle x MNR", "V1", 2024),
            ("Extras", "V1", 2023),
            ("Extras", "V3", 2025),
            ("Gulbeeneh Heels", "V1", 2023),
            ("Kurmayee", "V1", 2023),
            ("Leather Bags", "V1", 2024),
            ("Malika-e-Jahan", "V2", 2024),
            ("Masal", "V2", 2025),
            ("Mastani", "V1", 2025),
            ("Miraatul-Uroos", "V1", 2023),
            ("Mor Mahal", "V1", 2024),
            ("Old collection", "V1", 2019),
            ("On-the-Go Bags", "V1", 2024),
            ("Pazirai", "V1", 2023),
            ("SHADMANI", "V3", 2025),
            ("Sheharzaad", "V1", 2024),
            ("Summer Vibe", "V1", 2023),
            ("Zebtan", "V1", 2024),
            ("Zebtan", "V2", 2024),
            ("Zodiac Jewellery", "V1", 2023),
        ];

        var collections = rows.Select(r => new Collection { Name = r.Name, VersionLabel = r.Version, Year = r.Year }).ToArray();
        db.Collections.AddRange(collections);
        await db.SaveChangesAsync();

        var result = new Dictionary<string, Collection>();
        for (var i = 0; i < rows.Length; i++)
        {
            result[$"{rows[i].Name}-{rows[i].Version}-{rows[i].Year}"] = collections[i];
        }
        return result;
    }

    /// <summary>Returns option lookup keyed by "TYPE_CODE:OPTION_CODE" for use when assigning
    /// attribute values to seeded sample products.</summary>
    private static async Task<Dictionary<string, ProductAttributeOption>> SeedAttributeTypesAsync(
        AppDbContext db, Dictionary<string, Department> departments)
    {
        var upperMaterial = new ProductAttributeType { Code = "UPPER_MATERIAL", Name = "Upper Material", DisplayOrder = 1 };
        var upperType = new ProductAttributeType { Code = "UPPER_TYPE", Name = "Upper Type", DisplayOrder = 2 };
        var heelHeight = new ProductAttributeType { Code = "HEEL_HEIGHT", Name = "Heel Height", DisplayOrder = 3, DepartmentId = departments["FOOTWEAR"].Id };
        var heelType = new ProductAttributeType { Code = "HEEL_TYPE", Name = "Heel Type", DisplayOrder = 4, DepartmentId = departments["FOOTWEAR"].Id };
        var soleMaterial = new ProductAttributeType { Code = "SOLE_MATERIAL", Name = "Sole Material", DisplayOrder = 5, DepartmentId = departments["FOOTWEAR"].Id };
        var soleType = new ProductAttributeType { Code = "SOLE_TYPE", Name = "Sole Type", DisplayOrder = 6, DepartmentId = departments["FOOTWEAR"].Id };

        var types = new[] { upperMaterial, upperType, heelHeight, heelType, soleMaterial, soleType };
        db.ProductAttributeTypes.AddRange(types);
        await db.SaveChangesAsync();

        var options = new List<ProductAttributeOption>();
        void AddOptions(ProductAttributeType type, params string[] codes) =>
            options.AddRange(codes.Select(c => new ProductAttributeOption { ProductAttributeTypeId = type.Id, Code = c, Name = ToTitle(c) }));

        AddOptions(upperMaterial, "CANE", "JUTE", "METAL", "MICRO VELVET", "NET", "RAW SILK", "RAXINE", "SOFA VELVET");
        AddOptions(upperType, "DEFAULT", "CRISS_CROSS", "EMBELLISHED", "EMBELLISHED_NET", "EMBELLISHED_PRINTED", "EMBROIDERED_MOTIF", "METAL_ORNAMENT", "PLAIN", "PRINTED", "THREAD_EMBROIDERY");
        AddOptions(heelHeight, "DEFAULT", "FLAT_0_5MM", "SMALL_6_51MM", "MEDIUM_52_76MM");
        AddOptions(heelType, "DEFAULT", "FLAT", "KITTEN", "BLOCK");
        AddOptions(soleMaterial, "DEFAULT", "LEATHER", "SHEET");
        AddOptions(soleType, "DEFAULT", "KHUSSA_SOLE", "PEEP_TOE", "NARROW_TOE", "SEMI_BROAD_TOE");

        db.ProductAttributeOptions.AddRange(options);
        await db.SaveChangesAsync();

        var result = new Dictionary<string, ProductAttributeOption>();
        foreach (var type in types)
        {
            foreach (var option in options.Where(o => o.ProductAttributeTypeId == type.Id))
            {
                result[$"{type.Code}:{option.Code}"] = option;
            }
        }
        return result;
    }

    private static string ToTitle(string code) =>
        string.Join(' ', code.Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant() : w));

    private static async Task<(Dictionary<string, Store> Stores, Dictionary<string, Warehouse> Warehouses)> SeedStoresAndWarehousesAsync(AppDbContext db)
    {
        var flagship = new Store { Code = "ST-01", Name = "Flagship Store", City = "Lahore", Status = PartyStatus.Active };
        db.Stores.Add(flagship);
        await db.SaveChangesAsync();

        var central = new Warehouse { Code = "WH-CENTRAL", Name = "Central Distribution", City = "Lahore", Status = PartyStatus.Active };
        var flagshipBackroom = new Warehouse { Code = "WH-ST01", Name = "Flagship Backroom", City = "Lahore", Status = PartyStatus.Active, StoreId = flagship.Id };
        db.Warehouses.AddRange(central, flagshipBackroom);
        await db.SaveChangesAsync();

        return (
            new Dictionary<string, Store> { [flagship.Code] = flagship },
            new Dictionary<string, Warehouse> { [central.Code] = central, [flagshipBackroom.Code] = flagshipBackroom });
    }

    private static async Task SeedSampleProductsAsync(
        AppDbContext db,
        Dictionary<string, Department> departments,
        Dictionary<string, Gender> genders,
        Dictionary<string, EventType> eventTypes,
        Dictionary<(string, string), Category> categories,
        Dictionary<(string, string), Subcategory> subcategories,
        Dictionary<string, Collection> collections,
        Dictionary<string, ProductAttributeOption> attributeOptions,
        Dictionary<string, Warehouse> warehouses)
    {
        var byCategoryCode = categories.Values.GroupBy(c => c.Code).ToDictionary(g => g.Key, g => g.First());
        var bySubcategoryCode = subcategories.Values.GroupBy(s => s.Code).ToDictionary(g => g.Key, g => g.First());
        var warehouse = warehouses["WH-CENTRAL"];

        // (ItemCode, Dept, Gender, Event, Category, Subcategory, UpperMaterial, UpperType,
        //  HeelHeight, HeelType, SoleMaterial, SoleType, Collection, Year, Price)
        var rows = new[]
        {
            ("J23124001", "JEWELLERY", "WOMEN", "FORMAL", "EARRINGS", "Earings", "METAL", "DEFAULT", (string?)null, (string?)null, (string?)null, (string?)null, "Zebtan-V2-2024", 2024, 45.00m),
            ("J22424001", "JEWELLERY", "WOMEN", "CASUAL", "RING", "SLEEK FINGER RING", "METAL", "DEFAULT", null, null, null, null, "Dazzle Jewellery-V1-2024", 2024, 28.00m),
            ("J23724001", "JEWELLERY", "WOMEN", "FORMAL", "BANGLES", "BANGLES", "METAL", "DEFAULT", null, null, null, null, "Zebtan-V1-2024", 2024, 55.00m),
            ("B24123001", "BAGS", "WOMEN", "COTURE", "HAND CARRY", "CLUTCHES", "SOFA VELVET", "EMBELLISHED", null, null, null, null, "Miraatul-Uroos-V1-2023", 2023, 120.00m),
            ("B24125001", "BAGS", "WOMEN", "COTURE", "HAND CARRY", "POTLI", "RAW SILK", "EMBELLISHED", null, null, null, null, "Aashvi-V1-2024", 2024, 95.00m),
            ("B22324001", "BAGS", "WOMEN", "CASUAL", "SHOULDER", "TOTE BAG", "JUTE", "EMBROIDERED_MOTIF", null, null, null, null, "On-the-Go Bags-V1-2024", 2024, 65.00m),
            ("B21224001", "BAGS", "WOMEN", "BASIC", "CROSS BODY", "POTLI", "RAXINE", "PLAIN", null, null, null, null, "Aarinda Potli-V1-2024", 2024, 40.00m),
            ("F21619001", "FOOTWEAR", "WOMEN", "BASIC", "KHUSSA", "REGULAR", "RAXINE", "PLAIN", "FLAT_0_5MM", "FLAT", "LEATHER", "KHUSSA_SOLE", "Old collection-V1-2019", 2019, 32.00m),
            ("F24625001", "FOOTWEAR", "WOMEN", "COTURE", "KHUSSA", "REGULAR", "SOFA VELVET", "EMBELLISHED_PRINTED", "FLAT_0_5MM", "FLAT", "LEATHER", "KHUSSA_SOLE", "Masal-V2-2025", 2025, 68.00m),
            ("F23223001", "FOOTWEAR", "WOMEN", "FORMAL", "SANDAL", "SHELL TOE", "SOFA VELVET", "EMBELLISHED", "SMALL_6_51MM", "KITTEN", "SHEET", "PEEP_TOE", "Gulbeeneh Heels-V1-2023", 2023, 58.00m),
            ("F24223001", "FOOTWEAR", "WOMEN", "COTURE", "SANDAL", "NARROW BAND", "SOFA VELVET", "EMBELLISHED", "MEDIUM_52_76MM", "BLOCK", "SHEET", "NARROW_TOE", "Kurmayee-V1-2023", 2023, 72.00m),
            ("F22325001", "FOOTWEAR", "WOMEN", "CASUAL", "SLIPPER", "BROAD BAND", "CANE", "METAL_ORNAMENT", "FLAT_0_5MM", "FLAT", "SHEET", "SEMI_BROAD_TOE", "Extras-V3-2025", 2025, 38.00m),
            ("F23325001", "FOOTWEAR", "WOMEN", "FORMAL", "SLIPPER", "D3-BAND", "SOFA VELVET", "EMBELLISHED", "FLAT_0_5MM", "FLAT", "SHEET", "SEMI_BROAD_TOE", "SHADMANI-V3-2025", 2025, 48.00m),
            ("K24624002", "FOOTWEAR", "KIDS", "COTURE", "KHUSSA", "REGULAR", "SOFA VELVET", "EMBELLISHED", "FLAT_0_5MM", "FLAT", "LEATHER", "KHUSSA_SOLE", "Malika-e-Jahan-V2-2024", 2024, 42.00m),
        };

        var products = new List<Product>();
        foreach (var r in rows)
        {
            var (itemCode, dept, gender, evt, cat, subcat, upperMat, upperTyp, heelH, heelT, soleM, soleT, collectionKey, year, price) = r;

            var product = new Product
            {
                ItemCode = itemCode,
                Sku = itemCode,
                Name = $"{ToTitle(subcat.Replace("Default", cat))} - {ToTitle(cat)}",
                DepartmentId = departments[dept].Id,
                GenderId = genders[gender].Id,
                EventTypeId = eventTypes[evt].Id,
                CategoryId = byCategoryCode[cat].Id,
                SubcategoryId = bySubcategoryCode.TryGetValue(subcat, out var sc) ? sc.Id : null,
                CollectionId = collections.TryGetValue(collectionKey, out var coll) ? coll.Id : null,
                Year = year,
                Cost = Math.Round(price * 0.55m, 2),
                Price = price,
                TaxRatePercent = 5,
                Unit = "pc",
                MinStock = 5,
                ReorderLevel = 15,
                Status = ProductStatus.Active,
            };

            void AddAttr(string typeCode, string? optionCode)
            {
                if (optionCode is null) return;
                if (attributeOptions.TryGetValue($"{typeCode}:{optionCode}", out var option))
                {
                    product.AttributeValues.Add(new ProductAttributeValue { ProductAttributeTypeId = option.ProductAttributeTypeId, ProductAttributeOptionId = option.Id });
                }
            }

            AddAttr("UPPER_MATERIAL", upperMat);
            AddAttr("UPPER_TYPE", upperTyp);
            AddAttr("HEEL_HEIGHT", heelH);
            AddAttr("HEEL_TYPE", heelT);
            AddAttr("SOLE_MATERIAL", soleM);
            AddAttr("SOLE_TYPE", soleT);

            products.Add(product);
        }

        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var random = new Random(42);
        foreach (var product in products)
        {
            var qty = random.Next(10, 80);
            db.InventoryBalances.Add(new InventoryBalance { ProductId = product.Id, WarehouseId = warehouse.Id, Quantity = qty });
            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                QuantityDelta = qty,
                Kind = StockMovementKind.OpeningStock,
                Reference = product.Sku,
            });
        }
        await db.SaveChangesAsync();
    }
}
