using FluentValidation;

namespace RetailCommerce.Application.Settings;

public class UpdateCurrencySettingsRequestValidator : AbstractValidator<UpdateCurrencySettingsRequest>
{
    public UpdateCurrencySettingsRequestValidator()
    {
        RuleFor(x => x.Symbol).MaximumLength(10);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 4);
    }
}
