using FluentValidation;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Application.Customers;

public class UpsertCustomerRequestValidator : AbstractValidator<UpsertCustomerRequest>
{
    public UpsertCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Type)
            .Must(s => Enum.TryParse<CustomerType>(s, true, out _))
            .WithMessage("Type must be one of: Retail, Wholesale, Corporate, WalkIn.");
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<PartyStatus>(s, true, out _))
            .WithMessage("Status must be one of: Active, Inactive, OnHold.");
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}
