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

    public string FullName => LastName is { Length: > 0 } ? $"{FirstName} {LastName}" : FirstName;
}
