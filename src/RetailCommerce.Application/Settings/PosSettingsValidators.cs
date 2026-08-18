using FluentValidation;

namespace RetailCommerce.Application.Settings;

public class UpdatePosSettingsRequestValidator : AbstractValidator<UpdatePosSettingsRequest>
{
    public UpdatePosSettingsRequestValidator()
    {
        RuleFor(x => x.ViewMode).Must(v => v is "Default" or "List").WithMessage("ViewMode must be 'Default' or 'List'.");
        RuleFor(x => x.ReturnPolicyDays).InclusiveBetween(1, 365);
    }
}
