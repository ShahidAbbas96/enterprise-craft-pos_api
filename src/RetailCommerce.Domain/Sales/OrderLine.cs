using RetailCommerce.Domain.Catalog;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Sales;

/// <summary>Product name, unit price and tax rate are snapshotted at sale time — the order
/// must never silently change if the product is edited later.</summary>
public class OrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }
}
