using FluentValidation;

namespace RetailCommerce.Application.Sales;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        // No NotEmpty() rule on WarehouseId — a terminal-scoped POS caller legitimately omits it
        // and SalesService.CreateSaleAsync fills it from the token via ResolveWarehouseScope; a
        // back-office caller that also omits it gets a clear ConflictException from that same
        // call instead of a generic FluentValidation message.
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.DiscountLabel).MaximumLength(150);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Cart is empty.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
