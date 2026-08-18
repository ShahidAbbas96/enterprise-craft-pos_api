using FluentValidation;

namespace RetailCommerce.Application.Discounts;

public class UpsertDiscountRequestValidator : AbstractValidator<UpsertDiscountRequest>
{
    public UpsertDiscountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).Must(t => t is "Percentage" or "FixedAmount" or "FixedPrice").WithMessage("Type must be 'Percentage', 'FixedAmount', or 'FixedPrice'.");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.Type == "Percentage").WithMessage("Percentage discounts must be between 0 and 100.");
        // A fixed final price only makes sense for one specific product — a department-wide or
        // whole-cart discount can't have a single target price since every product in it starts
        // at a different price.
        RuleFor(x => x.ProductId).NotNull().When(x => x.Type == "FixedPrice").WithMessage("Discount Price requires a specific product.");
    }
}
