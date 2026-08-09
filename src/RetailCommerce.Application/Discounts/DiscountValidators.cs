using FluentValidation;

namespace RetailCommerce.Application.Discounts;

public class UpsertDiscountRequestValidator : AbstractValidator<UpsertDiscountRequest>
{
    public UpsertDiscountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).Must(t => t is "Percentage" or "FixedAmount").WithMessage("Type must be 'Percentage' or 'FixedAmount'.");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.Type == "Percentage").WithMessage("Percentage discounts must be between 0 and 100.");
    }
}
