using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Customers;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string? LastName,
    string FullName,
    string Phone,
    string? Email,
    string Type,
    string? Address,
    string? City,
    string? Country,
    DateOnly? DateOfBirth,
    string? TaxNumber,
    decimal CreditLimit,
    decimal OpeningBalance,
    decimal Balance,
    int LoyaltyPoints,
    int OrdersCount,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc);

public record UpsertCustomerRequest(
    string FirstName,
    string? LastName,
    string Phone,
    string? Email,
    string Type,
    string? Address,
    string? City,
    string? Country,
    DateOnly? DateOfBirth,
    string? TaxNumber,
    decimal CreditLimit,
    decimal OpeningBalance,
    string? Notes,
    string Status);

public class CustomerListQuery : PagedQuery
{
    public string? Type { get; set; }
    public string? Status { get; set; }

    /// <summary>Used by the offline-sync delta pull (SyncService.PullAsync) — null for every
    /// other caller, which keeps the unfiltered full-list behavior unchanged.</summary>
    public DateTimeOffset? UpdatedSince { get; set; }
}
