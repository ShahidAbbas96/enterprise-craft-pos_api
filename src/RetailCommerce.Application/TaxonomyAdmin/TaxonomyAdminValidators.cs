using FluentValidation;

namespace RetailCommerce.Application.TaxonomyAdmin;

public class UpsertTaxonomyItemRequestValidator : AbstractValidator<UpsertTaxonomyItemRequest>
{
    public UpsertTaxonomyItemRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpsertCategoryRequestValidator : AbstractValidator<UpsertCategoryRequest>
{
    public UpsertCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

public class UpsertSubcategoryRequestValidator : AbstractValidator<UpsertSubcategoryRequest>
{
    public UpsertSubcategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpsertCollectionRequestValidator : AbstractValidator<UpsertCollectionRequest>
{
    public UpsertCollectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.VersionLabel).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
    }
}
