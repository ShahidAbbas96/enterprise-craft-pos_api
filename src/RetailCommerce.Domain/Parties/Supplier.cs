using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Parties;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal Rating { get; set; } = 5;
    public decimal Balance { get; set; }
    public int LeadDays { get; set; } = 7;
    public PartyStatus Status { get; set; } = PartyStatus.Active;
}
