using FluentValidation;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Application.Suppliers;

public class UpsertSupplierRequestValidator : AbstractValidator<UpsertSupplierRequest>
{
    public UpsertSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Rating).InclusiveBetween(0, 5);
        RuleFor(x => x.LeadDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<PartyStatus>(s, true, out _))
            .WithMessage("Status must be one of: Active, Inactive, OnHold.");
    }
}
