using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Products;
using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Products;

public class ProductService(AppDbContext db, IBarcodeService barcodeService) : IProductService
{
    public async Task<PagedResult<ProductDto>> ListAsync(ProductListQuery query, CancellationToken ct = default)
    {
        var products = db.Products
            .Include(p => p.Department)
            .Include(p => p.Gender)
            .Include(p => p.EventType)
            .Include(p => p.Category)
            .Include(p => p.Subcategory)
            .Include(p => p.Collection)
            .Include(p => p.Supplier)
            .Include(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeType)
            .Include(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeOption)
            .AsQueryable();

        if (query.DepartmentId is { } dep) products = products.Where(p => p.DepartmentId == dep);
        if (query.GenderId is { } gen) products = products.Where(p => p.GenderId == gen);
        if (query.EventTypeId is { } evt) products = products.Where(p => p.EventTypeId == evt);
        if (query.CategoryId is { } cat) products = products.Where(p => p.CategoryId == cat);
        if (query.SubcategoryId is { } sub) products = products.Where(p => p.SubcategoryId == sub);
        if (query.CollectionId is { } col) products = products.Where(p => p.CollectionId == col);
        if (query.Year is { } yr) products = products.Where(p => p.Year == yr);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ProductStatus>(query.Status, true, out var status))
        {
            products = products.Where(p => p.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                EF.Functions.ILike(p.Sku, $"%{term}%") ||
                (p.Barcode != null && EF.Functions.ILike(p.Barcode, $"%{term}%")) ||
                (p.ItemCode != null && EF.Functions.ILike(p.ItemCode, $"%{term}%")));
        }

        var totalCount = await products.CountAsync(ct);

        var page = await products
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var productIds = page.Select(p => p.Id).ToList();
        var stockByProduct = await db.InventoryBalances
            .Where(i => productIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total, ct);

        var items = page.Select(p => ToDto(p, stockByProduct.GetValueOrDefault(p.Id), MapAttributes(p))).ToList();

        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<ProductDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var product = await LoadFullAsync(id, ct) ?? throw new NotFoundException("Product", id);
        var totalStock = await db.InventoryBalances.Where(i => i.ProductId == id).SumAsync(i => (int?)i.Quantity, ct) ?? 0;
        return ToDto(product, totalStock, MapAttributes(product));
    }

    public async Task<ProductDto> CreateAsync(UpsertProductRequest request, CancellationToken ct = default)
    {
        await ValidateFieldConfigAsync(request, ct);
        await EnsureTaxonomyReferencesExistAsync(request, ct);

        var product = new Product { Id = Guid.NewGuid() };
        MapRequestToEntity(request, product);

        // Sku is always generated unless the caller explicitly supplies one (Import trusts the
        // file's value verbatim; the Product form never sends one, so this always generates here).
        product.Sku = string.IsNullOrWhiteSpace(request.Sku)
            ? await GenerateSkuAsync(request, ct)
            : request.Sku.Trim();
        await EnsureSkuUniqueAsync(product.Sku, existingId: null, ct);

        product.ItemCode = string.IsNullOrWhiteSpace(request.ItemCode)
            ? await GenerateItemCodeAsync(product.Name, ct)
            : request.ItemCode.Trim();

        db.Products.Add(product);

        await ApplyAttributeValuesAsync(product, request.Attributes, ct);

        if (request.InitialStockQuantity is > 0 && request.InitialStockWarehouseId is { } warehouseId)
        {
            await EnsureWarehouseExistsAsync(warehouseId, ct);
            db.InventoryBalances.Add(new InventoryBalance { ProductId = product.Id, WarehouseId = warehouseId, Quantity = request.InitialStockQuantity.Value });
            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                WarehouseId = warehouseId,
                QuantityDelta = request.InitialStockQuantity.Value,
                Kind = StockMovementKind.OpeningStock,
                Reference = product.Sku,
            });
        }

        await db.SaveChangesAsync(ct);
        await barcodeService.EnsureCurrentBarcodeAsync(product.Id, ct);
        return await GetAsync(product.Id, ct);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpsertProductRequest request, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Product", id);
        await ValidateFieldConfigAsync(request, ct);
        await EnsureTaxonomyReferencesExistAsync(request, ct);

        MapRequestToEntity(request, product);

        // Sku/ItemCode are read-only in the Product form (it never sends them, so these branches
        // simply preserve whatever the product already has) — only an explicit value in the
        // request (e.g. Import correcting legacy data) overwrites them.
        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var trimmedSku = request.Sku.Trim();
            if (!string.Equals(trimmedSku, product.Sku, StringComparison.Ordinal))
            {
                await EnsureSkuUniqueAsync(trimmedSku, existingId: id, ct);
                product.Sku = trimmedSku;
            }
        }
        if (!string.IsNullOrWhiteSpace(request.ItemCode))
        {
            product.ItemCode = request.ItemCode.Trim();
        }

        await ApplyAttributeValuesAsync(product, request.Attributes, ct);

        await db.SaveChangesAsync(ct);
        await barcodeService.EnsureCurrentBarcodeAsync(product.Id, ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Product", id);
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProductFieldConfigDto>> GetFieldConfigAsync(CancellationToken ct = default)
    {
        var states = await GetFieldStatesAsync(ct);
        return ProductFieldCatalog.Fields
            .Select(f => new ProductFieldConfigDto(f.Key, f.DisplayName, states[f.Key].ToString()))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductFieldConfigDto>> UpdateFieldConfigAsync(IReadOnlyList<UpdateProductFieldConfigRequest> requests, CancellationToken ct = default)
    {
        var knownKeys = ProductFieldCatalog.Fields.Select(f => f.Key).ToHashSet();
        var existing = await db.ProductFieldConfigs.ToDictionaryAsync(c => c.FieldKey, ct);

        foreach (var req in requests)
        {
            if (!knownKeys.Contains(req.FieldKey))
            {
                throw new NotFoundException("ProductFieldConfig", req.FieldKey);
            }
            if (!Enum.TryParse<ProductFieldState>(req.State, ignoreCase: true, out var state))
            {
                throw new ConflictException($"'{req.State}' is not a valid field state. Use Required, Optional, or Hidden.");
            }

            if (existing.TryGetValue(req.FieldKey, out var row))
            {
                row.State = state;
            }
            else
            {
                db.ProductFieldConfigs.Add(new ProductFieldConfig { FieldKey = req.FieldKey, State = state });
            }
        }

        await db.SaveChangesAsync(ct);
        return await GetFieldConfigAsync(ct);
    }

    // ---- helpers ----

    private async Task<Product?> LoadFullAsync(Guid id, CancellationToken ct) =>
        await db.Products
            .Include(p => p.Department)
            .Include(p => p.Gender)
            .Include(p => p.EventType)
            .Include(p => p.Category)
            .Include(p => p.Subcategory)
            .Include(p => p.Collection)
            .Include(p => p.Supplier)
            .Include(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeType)
            .Include(p => p.AttributeValues).ThenInclude(v => v.ProductAttributeOption)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    private static List<ProductAttributeValueDto> MapAttributes(Product product) =>
        product.AttributeValues
            .Select(v => new ProductAttributeValueDto(
                v.ProductAttributeTypeId, v.ProductAttributeType.Code, v.ProductAttributeType.Name,
                v.ProductAttributeOptionId, v.ProductAttributeOption.Code, v.ProductAttributeOption.Name))
            .ToList();

    private static ProductDto ToDto(Product p, int totalStock, IReadOnlyList<ProductAttributeValueDto> attributes) => new(
        p.Id, p.ItemCode, p.Sku, p.Barcode, p.Name, p.Description, p.ImageUrl,
        p.DepartmentId, p.Department?.Name,
        p.GenderId, p.Gender?.Name,
        p.EventTypeId, p.EventType?.Name,
        p.CategoryId, p.Category?.Name,
        p.SubcategoryId, p.Subcategory?.Name,
        p.CollectionId, p.Collection?.DisplayCode,
        p.Year,
        p.SupplierId, p.Supplier?.Name,
        p.Cost, p.Price, p.WholesalePrice, p.TaxRatePercent, p.DiscountPercent, p.Unit,
        p.MinStock, p.MaxStock, p.ReorderLevel,
        p.Location, p.Status.ToString(), totalStock, attributes, p.CreatedAtUtc);

    private static void MapRequestToEntity(UpsertProductRequest r, Product p)
    {
        // Sku/ItemCode are assigned by the caller (Create/UpdateAsync), not here — generating
        // them needs an async sequence lookup this static mapper can't do.
        p.Name = r.Name.Trim();
        p.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        p.ImageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();
        p.DepartmentId = r.DepartmentId;
        p.GenderId = r.GenderId;
        p.EventTypeId = r.EventTypeId;
        p.CategoryId = r.CategoryId;
        p.SubcategoryId = r.SubcategoryId;
        p.CollectionId = r.CollectionId;
        p.Year = r.Year;
        p.SupplierId = r.SupplierId;
        p.Cost = r.Cost;
        p.Price = r.Price;
        p.WholesalePrice = r.WholesalePrice;
        p.TaxRatePercent = r.TaxRatePercent;
        p.DiscountPercent = r.DiscountPercent;
        p.Unit = r.Unit.Trim();
        p.MinStock = r.MinStock;
        p.MaxStock = r.MaxStock;
        p.ReorderLevel = r.ReorderLevel;
        p.Location = string.IsNullOrWhiteSpace(r.Location) ? null : r.Location.Trim();
        p.Status = Enum.Parse<ProductStatus>(r.Status, ignoreCase: true);
    }

    private async Task ApplyAttributeValuesAsync(Product product, IReadOnlyList<ProductAttributeValueInput> inputs, CancellationToken ct)
    {
        var existing = await db.ProductAttributeValues.Where(v => v.ProductId == product.Id).ToListAsync(ct);
        db.ProductAttributeValues.RemoveRange(existing);

        foreach (var input in inputs)
        {
            var optionBelongsToType = await db.ProductAttributeOptions
                .AnyAsync(o => o.Id == input.ProductAttributeOptionId && o.ProductAttributeTypeId == input.ProductAttributeTypeId, ct);
            if (!optionBelongsToType)
            {
                throw new ConflictException("One of the selected attribute values does not belong to its attribute type.");
            }

            db.ProductAttributeValues.Add(new Domain.Catalog.ProductAttributeValue
            {
                ProductId = product.Id,
                ProductAttributeTypeId = input.ProductAttributeTypeId,
                ProductAttributeOptionId = input.ProductAttributeOptionId,
            });
        }
    }

    private async Task EnsureSkuUniqueAsync(string sku, Guid? existingId, CancellationToken ct)
    {
        var skuTaken = await db.Products.AnyAsync(p => p.Sku == sku.Trim() && p.Id != existingId, ct);
        if (skuTaken) throw new ConflictException($"SKU '{sku}' is already in use.");
    }

    private async Task EnsureTaxonomyReferencesExistAsync(UpsertProductRequest r, CancellationToken ct)
    {
        if (r.DepartmentId is { } deptId && !await db.Departments.AnyAsync(x => x.Id == deptId, ct))
        {
            throw new NotFoundException("Department", deptId);
        }
        if (r.GenderId is { } genderId && !await db.Genders.AnyAsync(x => x.Id == genderId, ct))
        {
            throw new NotFoundException("Gender", genderId);
        }
        if (r.EventTypeId is { } eventTypeId && !await db.EventTypes.AnyAsync(x => x.Id == eventTypeId, ct))
        {
            throw new NotFoundException("EventType", eventTypeId);
        }

        if (r.CategoryId is { } categoryId)
        {
            var category = await db.Categories.FirstOrDefaultAsync(x => x.Id == categoryId, ct) ?? throw new NotFoundException("Category", categoryId);
            if (r.DepartmentId is { } deptForCategory && category.DepartmentId != deptForCategory)
            {
                throw new ConflictException("The selected category does not belong to the selected department.");
            }
        }

        if (r.SubcategoryId is { } subId)
        {
            var subcategory = await db.Subcategories.FirstOrDefaultAsync(x => x.Id == subId, ct) ?? throw new NotFoundException("Subcategory", subId);
            if (r.CategoryId is { } catForSub && subcategory.CategoryId != catForSub)
            {
                throw new ConflictException("The selected subcategory does not belong to the selected category.");
            }
        }

        if (r.CollectionId is { } colId && !await db.Collections.AnyAsync(x => x.Id == colId, ct))
        {
            throw new NotFoundException("Collection", colId);
        }

        if (r.SupplierId is { } supId && !await db.Suppliers.AnyAsync(x => x.Id == supId, ct))
        {
            throw new NotFoundException("Supplier", supId);
        }
    }

    private async Task EnsureWarehouseExistsAsync(Guid warehouseId, CancellationToken ct)
    {
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId, ct))
        {
            throw new NotFoundException("Warehouse", warehouseId);
        }
    }

    private async Task<Dictionary<string, ProductFieldState>> GetFieldStatesAsync(CancellationToken ct)
    {
        var saved = await db.ProductFieldConfigs.ToDictionaryAsync(c => c.FieldKey, c => c.State, ct);
        return ProductFieldCatalog.Fields.ToDictionary(f => f.Key, f => saved.GetValueOrDefault(f.Key, f.Default));
    }

    /// <summary>Defense in depth: the Product form already hides/requires fields per this same
    /// config, but a direct API call (or a stale client) must not be able to bypass it.</summary>
    private async Task ValidateFieldConfigAsync(UpsertProductRequest r, CancellationToken ct)
    {
        var states = await GetFieldStatesAsync(ct);
        var errors = new Dictionary<string, string[]>();

        void Require(string fieldKey, string requestPropertyName, bool isMissing)
        {
            if (isMissing && states[fieldKey] == ProductFieldState.Required)
            {
                var displayName = ProductFieldCatalog.Fields.First(f => f.Key == fieldKey).DisplayName;
                errors[requestPropertyName] = [$"{displayName} is required."];
            }
        }

        Require("Department", nameof(r.DepartmentId), r.DepartmentId is null);
        Require("Gender", nameof(r.GenderId), r.GenderId is null);
        Require("EventType", nameof(r.EventTypeId), r.EventTypeId is null);
        Require("Category", nameof(r.CategoryId), r.CategoryId is null);
        Require("Subcategory", nameof(r.SubcategoryId), r.SubcategoryId is null);
        Require("Collection", nameof(r.CollectionId), r.CollectionId is null);
        Require("Year", nameof(r.Year), r.Year is null);
        Require("Supplier", nameof(r.SupplierId), r.SupplierId is null);
        Require("WholesalePrice", nameof(r.WholesalePrice), r.WholesalePrice is null);
        Require("Location", nameof(r.Location), string.IsNullOrWhiteSpace(r.Location));
        Require("Description", nameof(r.Description), string.IsNullOrWhiteSpace(r.Description));
        Require("ImageUrl", nameof(r.ImageUrl), string.IsNullOrWhiteSpace(r.ImageUrl));
        // TaxRatePercent/DiscountPercent/MinStock/MaxStock/ReorderLevel are numeric with sensible
        // zero defaults — "Required" for these is a form-UX nudge only, not server-enforceable.

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }
    }

    /// <summary>"{Dept 2 letters}-{Color 3 letters}-{sequence}", e.g. "FO-BLA-000123". Missing
    /// segments fall back to fixed placeholders ("22"/"333") rather than guessing — this mirrors
    /// BarcodeService's MissingSegmentDefault convention. Department/Color text is derived the
    /// same way BarcodeService.DeriveShortCode does (letters/digits only, upper-cased, padded with
    /// 'X' if too short) for consistency across the two generators.</summary>
    private async Task<string> GenerateSkuAsync(UpsertProductRequest request, CancellationToken ct)
    {
        var deptSegment = "22";
        if (request.DepartmentId is { } deptId)
        {
            var deptName = await db.Departments.Where(d => d.Id == deptId).Select(d => d.Name).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(deptName)) deptSegment = ShortCode(deptName, 2);
        }

        var colorSegment = "333";
        var optionIds = request.Attributes.Select(a => a.ProductAttributeOptionId).ToList();
        if (optionIds.Count > 0)
        {
            var colorName = await db.ProductAttributeOptions
                .Where(o => optionIds.Contains(o.Id) && o.ProductAttributeType.Code == "COLOR")
                .Select(o => o.Name)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(colorName)) colorSegment = ShortCode(colorName, 3);
        }

        var sequence = await NextSequenceValueAsync("sku_seq", ct);
        return $"{deptSegment}-{colorSegment}-{sequence:D6}";
    }

    /// <summary>"{Name 3 letters}-{sequence}", e.g. "CLA-000045" for "Classic Sneaker".</summary>
    private async Task<string> GenerateItemCodeAsync(string name, CancellationToken ct)
    {
        var sequence = await NextSequenceValueAsync("item_code_seq", ct);
        return $"{ShortCode(name, 3)}-{sequence:D6}";
    }

    private static string ShortCode(string text, int length)
    {
        var letters = new string(text.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return letters.Length >= length ? letters[..length] : letters.PadRight(length, 'X');
    }

    /// <summary>Same raw-SQL nextval() pattern as DocumentNumberService — a real Postgres
    /// sequence, not an in-memory counter, so it's safe under concurrent requests.</summary>
    private async Task<long> NextSequenceValueAsync(string sequenceName, CancellationToken ct)
    {
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
        command.CommandText = $"SELECT nextval('{sequenceName}')";
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }
}
