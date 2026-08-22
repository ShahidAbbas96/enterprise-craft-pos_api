using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Parties;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = default!;
    public string? LastName { get; set; }
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public CustomerType Type { get; set; } = CustomerType.Retail;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? TaxNumber { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Balance { get; set; }
    public int LoyaltyPoints { get; set; }
    public int OrdersCount { get; set; }
    public string? Notes { get; set; }
    public PartyStatus Status { get; set; } = PartyStatus.Active;

    /// <summary>Client-generated (crypto.randomUUID()) idempotency key for offline-first POS
    /// sync — a retried submission of the same queued "quick add customer" carries the same
    /// value, letting CustomerService detect and safely no-op the duplicate. Null for customers
    /// created through the regular (always-online) back-office Customers screen.</summary>
    public Guid? ClientTransactionId { get; set; }

    public string FullName => LastName is { Length: > 0 } ? $"{FirstName} {LastName}" : FirstName;
}
