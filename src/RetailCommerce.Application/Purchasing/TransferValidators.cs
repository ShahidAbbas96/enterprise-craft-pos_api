using FluentValidation;

namespace RetailCommerce.Application.Purchasing;

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.FromWarehouseId).NotEmpty();
        RuleFor(x => x.ToWarehouseId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.FromWarehouseId != x.ToWarehouseId)
            .WithMessage("Source and destination warehouse must be different.")
            .WithName("ToWarehouseId");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one product line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
