using Microsoft.EntityFrameworkCore;
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

        var items = page.Select(p => ToDto(p, stockByProduct.GetValueOrDefault(p.Id), attributes: [])).ToList();

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
        await EnsureUniqueAsync(request.Sku, existingId: null, ct);
        await EnsureTaxonomyReferencesExistAsync(request, ct);

        var product = new Product { Id = Guid.NewGuid() };
        MapRequestToEntity(request, product);
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
        await EnsureUniqueAsync(request.Sku, existingId: id, ct);
        await EnsureTaxonomyReferencesExistAsync(request, ct);

        MapRequestToEntity(request, product);
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
        p.DepartmentId, p.Department.Name,
        p.GenderId, p.Gender.Name,
        p.EventTypeId, p.EventType.Name,
        p.CategoryId, p.Category.Name,
        p.SubcategoryId, p.Subcategory?.Name,
        p.CollectionId, p.Collection?.DisplayCode,
        p.Year,
        p.SupplierId, p.Supplier?.Name,
        p.Cost, p.Price, p.WholesalePrice, p.TaxRatePercent, p.DiscountPercent, p.Unit,
        p.MinStock, p.MaxStock, p.ReorderLevel,
        p.Location, p.Status.ToString(), totalStock, attributes, p.CreatedAtUtc);

    private static void MapRequestToEntity(UpsertProductRequest r, Product p)
    {
        p.ItemCode = string.IsNullOrWhiteSpace(r.ItemCode) ? null : r.ItemCode.Trim();
        p.Sku = r.Sku.Trim();
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

    private async Task EnsureUniqueAsync(string sku, Guid? existingId, CancellationToken ct)
    {
        var skuTaken = await db.Products.AnyAsync(p => p.Sku == sku.Trim() && p.Id != existingId, ct);
        if (skuTaken) throw new ConflictException($"SKU '{sku}' is already in use.");
    }

    private async Task EnsureTaxonomyReferencesExistAsync(UpsertProductRequest r, CancellationToken ct)
    {
        if (!await db.Departments.AnyAsync(x => x.Id == r.DepartmentId, ct)) throw new NotFoundException("Department", r.DepartmentId);
        if (!await db.Genders.AnyAsync(x => x.Id == r.GenderId, ct)) throw new NotFoundException("Gender", r.GenderId);
        if (!await db.EventTypes.AnyAsync(x => x.Id == r.EventTypeId, ct)) throw new NotFoundException("EventType", r.EventTypeId);

        var category = await db.Categories.FirstOrDefaultAsync(x => x.Id == r.CategoryId, ct) ?? throw new NotFoundException("Category", r.CategoryId);
        if (category.DepartmentId != r.DepartmentId)
        {
            throw new ConflictException("The selected category does not belong to the selected department.");
        }

        if (r.SubcategoryId is { } subId)
        {
            var subcategory = await db.Subcategories.FirstOrDefaultAsync(x => x.Id == subId, ct) ?? throw new NotFoundException("Subcategory", subId);
            if (subcategory.CategoryId != r.CategoryId)
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
}
