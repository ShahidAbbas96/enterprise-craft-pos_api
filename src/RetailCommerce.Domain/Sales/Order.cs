using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Inventory;
using RetailCommerce.Domain.Parties;

namespace RetailCommerce.Domain.Sales;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = default!;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public OrderChannel Channel { get; set; } = OrderChannel.Pos;
    public OrderStatus Status { get; set; } = OrderStatus.Completed;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    /// <summary>Snapshot text for the receipt (e.g. "14 August Sale (15%)" or "Manual
    /// discount") — independent of any Discount campaign row, which is just a POS picklist.</summary>
    public string? DiscountLabel { get; set; }

    public Guid? SalesPersonId { get; set; }
    public Employee? SalesPerson { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
