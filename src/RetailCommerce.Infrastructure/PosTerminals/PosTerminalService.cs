using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.PosTerminals;
using RetailCommerce.Domain.Sales;
using RetailCommerce.Infrastructure.Identity;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.PosTerminals;

public class PosTerminalService(AppDbContext db) : IPosTerminalService
{
    public async Task<IReadOnlyList<PosTerminalDto>> ListAsync(Guid? storeId = null, CancellationToken ct = default)
    {
        var terminals = db.PosTerminals
            .Include(t => t.Warehouse).ThenInclude(w => w.Store)
            .Include(t => t.AssignedUsers)
            .AsQueryable();

        if (storeId is { } id) terminals = terminals.Where(t => t.Warehouse.StoreId == id);

        var list = await terminals.OrderBy(t => t.Code).ToListAsync(ct);
        return await ToDtosAsync(list, ct);
    }

    public async Task<PosTerminalDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var terminal = await LoadAsync(id, ct);
        return (await ToDtosAsync([terminal], ct))[0];
    }

    public async Task<PosTerminalDto> CreateAsync(UpsertPosTerminalRequest request, CancellationToken ct = default)
    {
        await EnsureCodeUniqueAsync(request.Code, null, ct);
        await EnsureWarehouseHasStoreAsync(request.WarehouseId, ct);
        await EnsureUsersExistAsync(request.AssignedUserIds, ct);

        var terminal = new PosTerminal { Id = Guid.NewGuid() };
        MapRequestToEntity(request, terminal);
        db.PosTerminals.Add(terminal);
        await SyncAssignedUsersAsync(terminal.Id, request.AssignedUserIds, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(terminal.Id, ct);
    }

    public async Task<PosTerminalDto> UpdateAsync(Guid id, UpsertPosTerminalRequest request, CancellationToken ct = default)
    {
        var terminal = await db.PosTerminals.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("POS terminal", id);
        await EnsureCodeUniqueAsync(request.Code, id, ct);
        await EnsureWarehouseHasStoreAsync(request.WarehouseId, ct);
        await EnsureUsersExistAsync(request.AssignedUserIds, ct);

        MapRequestToEntity(request, terminal);
        await SyncAssignedUsersAsync(id, request.AssignedUserIds, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var terminal = await db.PosTerminals.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("POS terminal", id);
        db.PosTerminals.Remove(terminal);
        await db.SaveChangesAsync(ct);
    }

    private async Task<PosTerminal> LoadAsync(Guid id, CancellationToken ct) =>
        await db.PosTerminals
            .Include(t => t.Warehouse).ThenInclude(w => w.Store)
            .Include(t => t.AssignedUsers)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new NotFoundException("POS terminal", id);

    private async Task<IReadOnlyList<PosTerminalDto>> ToDtosAsync(IReadOnlyList<PosTerminal> terminals, CancellationToken ct)
    {
        var userIds = terminals.SelectMany(t => t.AssignedUsers.Select(u => u.UserId)).Distinct().ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        return terminals.Select(t => new PosTerminalDto(
            t.Id, t.Code, t.Name, t.WarehouseId, t.Warehouse.Name,
            t.Warehouse.StoreId, t.Warehouse.Store?.Name, t.IsActive,
            t.AssignedUsers
                .Select(a => users.GetValueOrDefault(a.UserId))
                .Where(u => u is not null)
                .Select(u => new PosTerminalUserDto(u!.Id, u.Email!, FullName(u)))
                .OrderBy(u => u.FullName)
                .ToList()))
            .ToList();
    }

    private static string FullName(ApplicationUser u) => u.LastName is { Length: > 0 } ? $"{u.FirstName} {u.LastName}" : u.FirstName;

    private async Task EnsureCodeUniqueAsync(string code, Guid? existingId, CancellationToken ct)
    {
        var taken = await db.PosTerminals.AnyAsync(t => t.Code == code.Trim() && t.Id != existingId, ct);
        if (taken) throw new ConflictException($"POS terminal code '{code}' is already in use.");
    }

    /// <summary>A terminal's Store is resolved transitively via Warehouse.Store — a warehouse
    /// with no Store (central/distribution warehouses are allowed to have one) can never back a
    /// terminal, or the whole claim-resolution chain used at login breaks.</summary>
    private async Task EnsureWarehouseHasStoreAsync(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId, ct)
            ?? throw new NotFoundException("Warehouse", warehouseId);
        if (warehouse.StoreId is null)
        {
            throw new ConflictException("That warehouse isn't linked to a store — link it to a store first.");
        }
    }

    private async Task EnsureUsersExistAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return;
        var found = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => u.Id).ToListAsync(ct);
        var missing = userIds.Except(found).ToList();
        if (missing.Count > 0) throw new NotFoundException("User", missing[0]);
    }

    private async Task SyncAssignedUsersAsync(Guid terminalId, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        var existing = await db.PosTerminalUsers.Where(x => x.TerminalId == terminalId).ToListAsync(ct);
        db.PosTerminalUsers.RemoveRange(existing);
        db.PosTerminalUsers.AddRange(userIds.Distinct().Select(uid => new PosTerminalUser { TerminalId = terminalId, UserId = uid }));
    }

    private static void MapRequestToEntity(UpsertPosTerminalRequest r, PosTerminal t)
    {
        t.Code = r.Code.Trim();
        t.Name = r.Name.Trim();
        t.WarehouseId = r.WarehouseId;
        t.IsActive = r.IsActive;
    }
}
