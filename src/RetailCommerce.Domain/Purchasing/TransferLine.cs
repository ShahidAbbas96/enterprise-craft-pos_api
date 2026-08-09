using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Purchasing;

public class TransferLine : BaseEntity
{
    public Guid TransferId { get; set; }
    public Transfer Transfer { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public int Quantity { get; set; }
    public string Unit { get; set; } = "pc";
}
