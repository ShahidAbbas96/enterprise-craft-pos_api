using FluentValidation;

namespace RetailCommerce.Application.AttributeAdmin;

public class UpsertAttributeTypeRequestValidator : AbstractValidator<UpsertAttributeTypeRequest>
{
    public UpsertAttributeTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpsertAttributeOptionRequestValidator : AbstractValidator<UpsertAttributeOptionRequest>
{
    public UpsertAttributeOptionRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
