using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Shifts;

public class Expense : BaseEntity
{
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = default!;

    public Guid ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = default!;

    public decimal Amount { get; set; }
    public string? Note { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
