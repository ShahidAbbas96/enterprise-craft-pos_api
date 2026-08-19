namespace RetailCommerce.Domain.Sales;

/// <summary>Many-to-many assignment of users to a POS terminal. UserId is a bare Guid (not a
/// navigation) since ApplicationUser lives in the Infrastructure/Identity layer, which Domain
/// must not reference — same convention already used by Order.CreatedByUserId.</summary>
public class PosTerminalUser
{
    public Guid TerminalId { get; set; }
    public PosTerminal Terminal { get; set; } = default!;

    public Guid UserId { get; set; }
}
