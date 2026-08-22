using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Customers;
using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Parties;
using RetailCommerce.Domain.Sync;
using RetailCommerce.Infrastructure.Common;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Customers;

public class CustomerService(AppDbContext db, ICurrentUserService currentUser) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken ct = default)
    {
        var customers = db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<CustomerType>(query.Type, true, out var type))
        {
            customers = customers.Where(c => c.Type == type);
        }
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PartyStatus>(query.Status, true, out var status))
        {
            customers = customers.Where(c => c.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            customers = customers.Where(c =>
                EF.Functions.ILike(c.FirstName, $"%{term}%") ||
                (c.LastName != null && EF.Functions.ILike(c.LastName, $"%{term}%")) ||
                EF.Functions.ILike(c.Phone, $"%{term}%") ||
                (c.Email != null && EF.Functions.ILike(c.Email, $"%{term}%")));
        }
        if (query.UpdatedSince is { } since)
        {
            customers = customers.Where(c => c.CreatedAtUtc > since || (c.UpdatedAtUtc != null && c.UpdatedAtUtc > since));
        }

        var totalCount = await customers.CountAsync(ct);
        var page = await customers
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = page.Select(ToDto).ToList();
        return new PagedResult<CustomerDto> { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Customer", id);
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(UpsertCustomerRequest request, CancellationToken ct = default)
    {
        // Fast-path pre-check first — before EnsurePhoneUniqueAsync, so a retried offline
        // "quick add customer" never trips the phone-uniqueness rule against its own earlier
        // attempt. Mirrors SalesService.CreateSaleAsync's idempotency pattern exactly.
        if (request.ClientTransactionId is { } precheckKey)
        {
            var existing = await db.Customers.FirstOrDefaultAsync(c => c.ClientTransactionId == precheckKey, ct);
            if (existing is not null)
            {
                await LogSyncAsync(existing.Id, precheckKey, SyncLogStatus.Duplicate, null, ct);
                return ToDto(existing);
            }
        }

        await EnsurePhoneUniqueAsync(request.Phone, null, ct);

        var customer = new Customer { Id = Guid.NewGuid(), ClientTransactionId = request.ClientTransactionId };
        MapRequestToEntity(request, customer);
        customer.Balance = request.OpeningBalance;
        db.Customers.Add(customer);

        if (request.ClientTransactionId is not null)
        {
            db.SyncLogs.Add(new SyncLog
            {
                TerminalId = currentUser.TerminalId,
                Direction = SyncDirection.Push,
                EntityType = "Customer",
                EntityId = customer.Id,
                ClientTransactionId = request.ClientTransactionId,
                Status = SyncLogStatus.Success,
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolationOn("IX_Customers_ClientTransactionId"))
        {
            // The real safety net: a concurrent retry raced this one past the pre-check above.
            db.ChangeTracker.Clear();
            var winner = await db.Customers.FirstAsync(c => c.ClientTransactionId == request.ClientTransactionId, ct);
            await LogSyncAsync(winner.Id, request.ClientTransactionId, SyncLogStatus.Duplicate, null, ct);
            return ToDto(winner);
        }

        return ToDto(customer);
    }

    private async Task LogSyncAsync(Guid customerId, Guid? clientTransactionId, SyncLogStatus status, string? errorMessage, CancellationToken ct)
    {
        db.SyncLogs.Add(new SyncLog
        {
            TerminalId = currentUser.TerminalId,
            Direction = SyncDirection.Push,
            EntityType = "Customer",
            EntityId = customerId,
            ClientTransactionId = clientTransactionId,
            Status = status,
            ErrorMessage = errorMessage,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpsertCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Customer", id);
        await EnsurePhoneUniqueAsync(request.Phone, id, ct);
        MapRequestToEntity(request, customer);
        await db.SaveChangesAsync(ct);
        return ToDto(customer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Customer", id);
        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsurePhoneUniqueAsync(string phone, Guid? existingId, CancellationToken ct)
    {
        var taken = await db.Customers.AnyAsync(c => c.Phone == phone.Trim() && c.Id != existingId, ct);
        if (taken) throw new ConflictException($"A customer with phone '{phone}' already exists.");
    }

    private static void MapRequestToEntity(UpsertCustomerRequest r, Customer c)
    {
        c.FirstName = r.FirstName.Trim();
        c.LastName = string.IsNullOrWhiteSpace(r.LastName) ? null : r.LastName.Trim();
        c.Phone = r.Phone.Trim();
        c.Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim();
        c.Type = Enum.Parse<CustomerType>(r.Type, true);
        c.Address = string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim();
        c.City = string.IsNullOrWhiteSpace(r.City) ? null : r.City.Trim();
        c.Country = string.IsNullOrWhiteSpace(r.Country) ? null : r.Country.Trim();
        c.DateOfBirth = r.DateOfBirth;
        c.TaxNumber = string.IsNullOrWhiteSpace(r.TaxNumber) ? null : r.TaxNumber.Trim();
        c.CreditLimit = r.CreditLimit;
        c.OpeningBalance = r.OpeningBalance;
        c.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        c.Status = Enum.Parse<PartyStatus>(r.Status, true);
    }

    private static CustomerDto ToDto(Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.FullName, c.Phone, c.Email, c.Type.ToString(),
        c.Address, c.City, c.Country, c.DateOfBirth, c.TaxNumber,
        c.CreditLimit, c.OpeningBalance, c.Balance, c.LoyaltyPoints, c.OrdersCount,
        c.Notes, c.Status.ToString(), c.CreatedAtUtc, c.ClientTransactionId);
}
