using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Sales;

public class ReturnLine : BaseEntity
{
    public Guid ReturnId { get; set; }
    public Return Return { get; set; } = default!;

    public Guid OrderLineId { get; set; }
    public OrderLine OrderLine { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
