using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Discounts;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Sales;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Discounts;

public class DiscountService(AppDbContext db) : IDiscountService
{
    public async Task<IReadOnlyList<DiscountDto>> ListAsync(bool activeOnly, DateTimeOffset? updatedSince = null, CancellationToken ct = default)
    {
        var query = Query();
        if (activeOnly) query = query.Where(d => d.IsActive);
        if (updatedSince is { } since) query = query.Where(d => d.CreatedAtUtc > since || (d.UpdatedAtUtc != null && d.UpdatedAtUtc > since));
        var entities = await query.OrderBy(d => d.Name).ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<DiscountDto> CreateAsync(UpsertDiscountRequest request, CancellationToken ct = default)
    {
        var type = ParseType(request.Type);
        await ValidateTargetsAsync(request.DepartmentId, request.ProductId, type, request.Value, ct);

        var entity = new Discount
        {
            Name = request.Name.Trim(),
            Type = type,
            Value = request.Value,
            IsActive = request.IsActive,
            DepartmentId = request.DepartmentId,
            ProductId = request.ProductId,
        };
        db.Discounts.Add(entity);
        await db.SaveChangesAsync(ct);
        return await GetDtoAsync(entity.Id, ct);
    }

    public async Task<DiscountDto> UpdateAsync(Guid id, UpsertDiscountRequest request, CancellationToken ct = default)
    {
        var entity = await db.Discounts.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new NotFoundException("Discount", id);
        var type = ParseType(request.Type);
        await ValidateTargetsAsync(request.DepartmentId, request.ProductId, type, request.Value, ct);

        entity.Name = request.Name.Trim();
        entity.Type = type;
        entity.Value = request.Value;
        entity.IsActive = request.IsActive;
        entity.DepartmentId = request.DepartmentId;
        entity.ProductId = request.ProductId;
        await db.SaveChangesAsync(ct);
        return await GetDtoAsync(entity.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Discounts.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new NotFoundException("Discount", id);
        db.Discounts.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<Discount> Query() => db.Discounts.Include(d => d.Department).Include(d => d.Product);

    private async Task<DiscountDto> GetDtoAsync(Guid id, CancellationToken ct)
    {
        var entity = await Query().FirstAsync(d => d.Id == id, ct);
        return ToDto(entity);
    }

    private async Task ValidateTargetsAsync(Guid? departmentId, Guid? productId, DiscountValueType type, decimal value, CancellationToken ct)
    {
        if (departmentId is { } deptId && !await db.Departments.AnyAsync(d => d.Id == deptId, ct))
        {
            throw new NotFoundException("Department", deptId);
        }
        if (productId is { } prodId)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == prodId, ct) ?? throw new NotFoundException("Product", prodId);
            // FixedPrice's Value is the target final price, so it only makes sense below the
            // product's current price — otherwise it's not a discount at all.
            if (type == DiscountValueType.FixedPrice && value >= product.Price)
            {
                throw new ConflictException($"Discount price ({value}) must be less than {product.Name}'s current price ({product.Price}).");
            }
        }
    }

    private static DiscountValueType ParseType(string type) =>
        Enum.TryParse<DiscountValueType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ConflictException($"Unknown discount type '{type}'.");

    private static DiscountDto ToDto(Discount d) => new(
        d.Id, d.Name, d.Type.ToString(), d.Value, d.IsActive,
        d.DepartmentId, d.Department?.Name, d.ProductId, d.Product?.Name);
}
