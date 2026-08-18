using FluentValidation;
using RetailCommerce.Domain.Common;

namespace RetailCommerce.Application.Products;

public class UpsertProductRequestValidator : AbstractValidator<UpsertProductRequest>
{
    public UpsertProductRequestValidator()
    {
        // Name/Cost/Price/Unit/Status stay unconditionally required here — those, plus Sku/ItemCode,
        // are the fixed core fields ProductFieldConfig can never hide (see ProductFieldConfig's doc
        // comment). Every other field's Required/Optional/Hidden enforcement is data-driven from
        // ProductFieldConfig, checked in ProductService — not hard-coded here, since it's admin-configurable.
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).MaximumLength(50);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WholesalePrice).GreaterThanOrEqualTo(0).When(x => x.WholesalePrice is not null);
        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStock).GreaterThanOrEqualTo(0).When(x => x.MaxStock is not null);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<ProductStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be one of: Draft, Active, Inactive.");
        RuleFor(x => x.InitialStockQuantity).GreaterThanOrEqualTo(0).When(x => x.InitialStockQuantity is not null);
        RuleFor(x => x.InitialStockWarehouseId)
            .NotEmpty()
            .WithMessage("A warehouse is required when setting an initial stock quantity.")
            .When(x => x.InitialStockQuantity is > 0);
    }
}

public class UpdateBarcodeSettingsRequestValidator : AbstractValidator<UpdateBarcodeSettingsRequest>
{
    public UpdateBarcodeSettingsRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LabelWidthInches).GreaterThan(0).LessThanOrEqualTo(20);
        RuleFor(x => x.LabelHeightInches).GreaterThan(0).LessThanOrEqualTo(20);
    }
}

public class AddProductBarcodeRequestValidator : AbstractValidator<AddProductBarcodeRequest>
{
    public AddProductBarcodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
    }
}
