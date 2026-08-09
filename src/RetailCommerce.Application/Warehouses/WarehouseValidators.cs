using FluentValidation;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Application.Warehouses;

public class UpsertWarehouseRequestValidator : AbstractValidator<UpsertWarehouseRequest>
{
    public UpsertWarehouseRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<PartyStatus>(s, true, out _))
            .WithMessage("Status must be one of: Active, Inactive, OnHold.");
    }
}
