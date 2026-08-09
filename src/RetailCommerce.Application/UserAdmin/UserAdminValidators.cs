using FluentValidation;
using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.UserAdmin;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Select at least one role.");
        RuleForEach(x => x.Roles).Must(r => Roles.All.Contains(r)).WithMessage("Unknown role.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Select at least one role.");
        RuleForEach(x => x.Roles).Must(r => Roles.All.Contains(r)).WithMessage("Unknown role.");
    }
}
