namespace RetailCommerce.Application.Products;

public record BarcodeSettingsDto(
    string CompanyName,
    bool IncludeCompanyName,
    bool IncludePrice,
    decimal LabelWidthInches,
    decimal LabelHeightInches);

public record UpdateBarcodeSettingsRequest(
    string CompanyName,
    bool IncludeCompanyName,
    bool IncludePrice,
    decimal LabelWidthInches,
    decimal LabelHeightInches);
