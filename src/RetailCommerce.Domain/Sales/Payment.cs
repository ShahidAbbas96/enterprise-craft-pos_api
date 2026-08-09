using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Sales;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
}
