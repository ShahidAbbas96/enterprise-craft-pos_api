using FluentValidation;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Application.Stores;

public class UpsertStoreRequestValidator : AbstractValidator<UpsertStoreRequest>
{
    public UpsertStoreRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Ntn).MaximumLength(30);
        RuleFor(x => x.Strn).MaximumLength(30);
        RuleFor(x => x.ReceiptFooterText).MaximumLength(2000);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<PartyStatus>(s, true, out _))
            .WithMessage("Status must be one of: Active, Inactive, OnHold.");
    }
}
