using FluentValidation;

namespace RetailCommerce.Application.Shifts;

public class OpenShiftRequestValidator : AbstractValidator<OpenShiftRequest>
{
    public OpenShiftRequestValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}

public class AddExpenseRequestValidator : AbstractValidator<AddExpenseRequest>
{
    public AddExpenseRequestValidator()
    {
        RuleFor(x => x.ExpenseCategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(300);
    }
}
